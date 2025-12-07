# %% [code] {"execution":{"iopub.status.busy":"2025-11-27T06:12:40.212213Z","iopub.execute_input":"2025-11-27T06:12:40.212958Z","iopub.status.idle":"2025-11-27T06:12:53.736497Z","shell.execute_reply.started":"2025-11-27T06:12:40.212921Z","shell.execute_reply":"2025-11-27T06:12:53.735687Z"}}
# ===== Cell 0: Làm sạch Kaggle env + cài stack tối thiểu (tránh SciPy/TF) =====
import os, sys, subprocess, importlib

# Chặn Transformers đụng tới TF/Flax
os.environ["TRANSFORMERS_NO_TF"] = "1"
os.environ["TRANSFORMERS_NO_FLAX"] = "1"
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "3"

def pip_uninstall(*pkgs):
    if not pkgs: 
        return
    try:
        subprocess.check_call([sys.executable, "-m", "pip", "uninstall", "-y", "-q", *pkgs])
    except subprocess.CalledProcessError:
        pass

def pip_install(*pkgs):
    subprocess.check_call([sys.executable, "-m", "pip", "install", "-q", "--upgrade", "--no-warn-script-location", *pkgs])

# Gỡ các gói dễ gây xung đột khi import transformers/diffusers
pip_uninstall(
    "scipy", "scikit-learn",
    "tensorflow", "tensorflow-cpu", "tensorflow-intel", "keras",
    "jax", "jaxlib"
)

# Cài gói “tối thiểu” đủ chạy pipeline
pip_install(
    "numpy==1.26.4",
    "ultralytics==8.3.225",
    "diffusers==0.31.0",
    "transformers==4.46.3",
    "accelerate==0.34.2",
    "safetensors==0.4.5",
    "opencv-python-headless==4.10.0.84",
    "onnxruntime==1.18.0",
    "rembg==2.0.57",
    "requests==2.32.3"
)

# Kiểm tra SciPy/TF đã thật sự biến mất
def _absent(modname):
    try:
        importlib.import_module(modname)
        print(f">> CẢNH BÁO: {modname} vẫn còn!")
    except Exception:
        print(f"{modname}: không có (OK)")

import numpy as _np
print("NumPy:", _np.__version__)
_absent("scipy")
_absent("sklearn")
_absent("tensorflow")
_absent("keras")


# %% [code] {"execution":{"iopub.status.busy":"2025-11-27T06:12:53.737440Z","iopub.execute_input":"2025-11-27T06:12:53.737685Z","iopub.status.idle":"2025-11-27T06:12:58.513261Z","shell.execute_reply.started":"2025-11-27T06:12:53.737660Z","shell.execute_reply":"2025-11-27T06:12:58.512476Z"}}
# ===== Cell 1: Imports & Config =====
import os, io, math, requests, random
from pathlib import Path
from typing import Tuple, Optional

import torch
from PIL import Image, ImageOps
import numpy as np
import cv2

from ultralytics import YOLO
from diffusers import StableDiffusionControlNetPipeline, ControlNetModel, DDIMScheduler
from transformers import CLIPTokenizer, CLIPTextModel

# Thiết bị
device = "cuda" if torch.cuda.is_available() else "cpu"
dtype  = torch.float16 if device == "cuda" else torch.float32
print("Device:", device, "| DType:", dtype)

# Thư mục làm việc
WORK = Path("/kaggle/working")
(IMAGES := WORK / "tryon_cache").mkdir(parents=True, exist_ok=True)

# Random seed cho reproducibility
SEED = 42
random.seed(SEED); np.random.seed(SEED); torch.manual_seed(SEED)
if device == "cuda":
    torch.cuda.manual_seed_all(SEED)


# %% [code] {"execution":{"iopub.status.busy":"2025-11-27T06:50:38.488771Z","iopub.execute_input":"2025-11-27T06:50:38.489440Z","iopub.status.idle":"2025-11-27T06:50:54.442383Z","shell.execute_reply.started":"2025-11-27T06:50:38.489415Z","shell.execute_reply":"2025-11-27T06:50:54.441441Z"}}
# ===== Cell 2: Load YOLO & ControlNet Pipeline =====

# 1) YOLO11n (COCO) — detect chó/mèo/người (IDs theo COCO)
yolo = YOLO("yolo11n.pt")  # auto-download
DOG_CAT_PERSON = [0, 15, 16]  # person=0, cat=15, dog=16

# 2) ControlNet (Canny) + SD1.5
#    - Base: "runwayml/stable-diffusion-v1-5" (ổn định, nhẹ hơn SDXL)
#    - ControlNet: "lllyasviel/sd-controlnet-canny"
BASE_MODEL = "Lykon/dreamshaper-8"
CANNY_MODEL = "lllyasviel/sd-controlnet-canny"

controlnet = ControlNetModel.from_pretrained(
    CANNY_MODEL, torch_dtype=dtype
)

pipe = StableDiffusionControlNetPipeline.from_pretrained(
    BASE_MODEL,
    controlnet=controlnet,
    torch_dtype=dtype,
    safety_checker=None
)

# Lịch DDIM cho ảnh gọn hơn
pipe.scheduler = DDIMScheduler.from_config(pipe.scheduler.config)

if device == "cuda":
    pipe.enable_model_cpu_offload()  # tiết kiệm VRAM
pipe.enable_vae_tiling()

print("Models loaded ✔")


# %% [code] {"execution":{"iopub.status.busy":"2025-11-27T06:13:01.751615Z","iopub.execute_input":"2025-11-27T06:13:01.751864Z","iopub.status.idle":"2025-11-27T06:13:01.766130Z","shell.execute_reply.started":"2025-11-27T06:13:01.751845Z","shell.execute_reply":"2025-11-27T06:13:01.765270Z"}}
# ===== Cell 3: Utils =====
from IPython.display import display

def load_image(src: str | Path) -> Image.Image:
    """Tải ảnh từ URL hoặc local path."""
    s = str(src)
    if s.startswith("http://") or s.startswith("https://"):
        r = requests.get(s, timeout=20)
        r.raise_for_status()
        return Image.open(io.BytesIO(r.content)).convert("RGB")
    return Image.open(s).convert("RGB")

def yolo_crop_subject(img: Image.Image,
                      classes=DOG_CAT_PERSON,
                      conf=0.35,
                      pad_ratio=0.08) -> Optional[Image.Image]:
    """Detect subject (chó/mèo/người). Trả về crop lớn nhất (ưu tiên chó/mèo)."""
    res = yolo.predict(img, classes=classes, conf=conf, verbose=False)[0]
    if res.boxes is None or res.boxes.shape[0] == 0:
        return None

    # Ưu tiên dog/cat, nếu không có thì lấy box lớn nhất
    boxes = res.boxes.xyxy.cpu().numpy().astype(int)
    clses = res.boxes.cls.cpu().numpy().astype(int)

    pri = [i for i,c in enumerate(clses) if c in (15,16)]  # cat/dog
    idxs = pri if len(pri)>0 else list(range(len(boxes)))

    # lấy box to nhất
    best = max(idxs, key=lambda i: (boxes[i][2]-boxes[i][0])*(boxes[i][3]-boxes[i][1]))
    x1,y1,x2,y2 = boxes[best]
    W,H = img.size
    pad = int(max(W,H)*pad_ratio)
    x1 = max(0, x1 - pad); y1 = max(0, y1 - pad)
    x2 = min(W, x2 + pad); y2 = min(H, y2 + pad)
    if x2<=x1 or y2<=y1: return None
    return img.crop((x1,y1,x2,y2))

def canny_from_pil(img: Image.Image,
                   low=100, high=200) -> Image.Image:
    """Tạo ảnh Canny từ PIL → PIL (RGB)"""
    arr = np.array(img)
    arr = cv2.cvtColor(arr, cv2.COLOR_RGB2GRAY)
    edges = cv2.Canny(arr, low, high)
    edges = np.stack([edges]*3, axis=-1)  # 3 kênh
    return Image.fromarray(edges)

def remove_bg(img: Image.Image) -> Image.Image:
    """
    Xóa nền sản phẩm, luôn trả về RGBA.
    Hỗ trợ mọi kiểu trả về của rembg (PIL.Image | bytes | numpy array).
    Nếu ảnh đã có alpha (PNG trong suốt), giữ nguyên.
    """
    # Nếu đã có alpha & có pixel trong suốt -> dùng luôn
    if img.mode in ("RGBA", "LA"):
        try:
            alpha = img.split()[-1]
            if np.array(alpha).min() < 255:
                return img.convert("RGBA")
        except Exception:
            pass

    from rembg import remove as _rembg_remove
    out = _rembg_remove(img)  # trên Kaggle hiện trả về PIL.Image

    # Trường hợp 1: rembg trả về PIL.Image
    if isinstance(out, Image.Image):
        return out.convert("RGBA")

    # Trường hợp 2: rembg trả về bytes
    if isinstance(out, (bytes, bytearray)):
        return Image.open(io.BytesIO(out)).convert("RGBA")

    # Trường hợp 3: rembg trả về numpy array
    if isinstance(out, np.ndarray):
        return Image.fromarray(out).convert("RGBA")

    # Nếu có kiểu lạ, báo rõ để còn xử lý
    raise TypeError(f"Unsupported rembg output type: {type(out)}")


def center_pad_resize(img: Image.Image, size=768) -> Image.Image:
    """Đưa ảnh về canvas vuông size×size (giữ tỉ lệ, padding)."""
    return ImageOps.pad(img, (size, size), method=Image.BICUBIC, color=(255,255,255), centering=(0.5,0.5))

def show_row(title: str, images: list[Tuple[str, Image.Image]], max_w=256):
    print(title)
    for name, im in images:
        print("-", name)
        display(im.resize((max_w, int(im.height*max_w/im.width))))


# %% [code] {"execution":{"iopub.status.busy":"2025-11-27T06:51:45.720789Z","iopub.execute_input":"2025-11-27T06:51:45.721089Z","iopub.status.idle":"2025-11-27T06:51:45.726053Z","shell.execute_reply.started":"2025-11-27T06:51:45.721067Z","shell.execute_reply":"2025-11-27T06:51:45.725438Z"}}
# ===== Cell 4: Cấu hình đầu vào =====

# Ảnh input
USER_IMAGE    = "https://www.tournhatban.net.vn/images/camnangdulich/tintucdulich/shiba/shiba-3.jpg"
PRODUCT_IMAGE = "/kaggle/input/datatesttask2/batancham.png"

# ——— Mô tả bằng tiếng Anh (SD hiểu tốt hơn) ———
# Bạn có thể sửa lại PRODUCT_NAME_EN cho từng sản phẩm:
PRODUCT_NAME_EN = "a strawberry-shaped slow feeder dog bowl"
# Ví dụ khác:
#   collar: "a red pet collar with a small bell"
#   muzzle: "a pink soft muzzle for dogs"
#   training bell: "a colorful set of training bells for dogs"

BASE_SUBJECT_EN = (
    "a single dog standing on the ground, full body, facing the camera"
)
SCENE_HINT_EN = "outdoor park, grass on the ground"
STYLE_HINT_EN = "cute pastel anime illustration, chibi style, high quality, soft shading"

# Prompt sinh ảnh (sẽ ghép trong Cell 5)
#   -> tạm thời để trống ở đây, tí nữa build trong Cell 5

# Tham số sinh ảnh
OUT_SIZE   = 768
GUIDANCE   = 8.5         # đẩy text influence mạnh hơn
STEPS      = 24          # nhiều step hơn chút
CTRL_SCALE = 0.8         # độ mạnh ControlNet (0.5–1.0, cứ 0.7–0.8 trước)
NUM_SAMPLES = 3          # sinh 3 ảnh để chọn
NEGATIVE = (
    "low quality, blurry, bad anatomy, deformed, "
    "extra legs, extra heads, extra animals, horse, human, "
    "cropped, cut off, text, watermark, logo, "
    "missing bowl, no bowl, empty ground"
)


# %% [code] {"execution":{"iopub.status.busy":"2025-11-27T06:51:48.428949Z","iopub.execute_input":"2025-11-27T06:51:48.429629Z","iopub.status.idle":"2025-11-27T06:52:35.950709Z","shell.execute_reply.started":"2025-11-27T06:51:48.429604Z","shell.execute_reply":"2025-11-27T06:52:35.949873Z"}}
# ===== Cell 5: Inference =====
# 1) Load ảnh
try:
    img_user = load_image(USER_IMAGE)
except Exception as e:
    raise RuntimeError(f"Không tải được USER_IMAGE: {e}")

try:
    img_prod = load_image(PRODUCT_IMAGE)
except Exception as e:
    raise RuntimeError(f"Không tải được PRODUCT_IMAGE: {e}")

# 2) Detect & crop subject
crop = yolo_crop_subject(img_user, conf=0.35)
if crop is None:
    raise RuntimeError("YOLO không phát hiện chó/mèo/người trong ảnh. Hãy thử ảnh khác rõ chủ thể hơn.")

# 3) Làm sạch sản phẩm (xóa nền), chuẩn hóa kích thước
prod_rgba = remove_bg(img_prod)  # RGBA (hiện giờ ta chỉ hiển thị cho người xem)
crop_std  = center_pad_resize(crop, OUT_SIZE)

# 4) Tạo Canny từ crop (là điều kiện cho ControlNet)
cond = canny_from_pil(crop_std, low=100, high=200)

show_row("Tiền xử lý", [
    ("Ảnh người dùng (crop)", crop),
    ("Canny từ crop", cond),
    ("Sản phẩm RGBA", prod_rgba.convert("RGB"))
])

# 5) Xây prompt
prompt = (
    f"{BASE_SUBJECT_EN}, "
    f"{SCENE_HINT_EN}, "
    f"the dog is interacting with {PRODUCT_NAME_EN}, "
    f"{PRODUCT_NAME_EN} is clearly visible in front of the dog, "
    f"the scene must show {PRODUCT_NAME_EN} on the ground, "
    f"{STYLE_HINT_EN}"
)

print("Prompt dùng để gen:\n", prompt, "\n")

# 6) Gọi ControlNet pipeline – sinh nhiều ảnh để chọn
generator = torch.Generator(device=device).manual_seed(SEED)
result = pipe(
    prompt=prompt,
    negative_prompt=NEGATIVE,
    image=cond,                         # ảnh điều kiện (Canny)
    num_inference_steps=STEPS,
    guidance_scale=GUIDANCE,
    generator=generator,
    controlnet_conditioning_scale=CTRL_SCALE,
    num_images_per_prompt=NUM_SAMPLES,  # sinh 3 ảnh
)

images = result.images

# 7) Hiển thị kết quả
rows = [(f"Ảnh sinh ra #{i+1}", img) for i, img in enumerate(images)]
show_row("Kết quả sinh ảnh", rows)

# Lưu ảnh đầu tiên (nếu muốn)
out_path = WORK / "tryon_result_1.png"
images[0].save(out_path, quality=95)
print("Saved ảnh #1:", out_path)


# %% [markdown]
# # ***Phía dưới là inference cũ*** -------------------------------------------------------------------------

# %% [code] {"execution":{"iopub.status.busy":"2025-11-27T06:31:24.309367Z","iopub.execute_input":"2025-11-27T06:31:24.309941Z","iopub.status.idle":"2025-11-27T06:31:45.754872Z","shell.execute_reply.started":"2025-11-27T06:31:24.309917Z","shell.execute_reply":"2025-11-27T06:31:45.754171Z"}}
# ===== Cell 5: Inference (ControlNet-Canny) =====

# 1) Load ảnh
try:
    img_user = load_image(USER_IMAGE)
except Exception as e:
    raise RuntimeError(f"Không tải được USER_IMAGE: {e}")

try:
    img_prod = load_image(PRODUCT_IMAGE)
except Exception as e:
    raise RuntimeError(f"Không tải được PRODUCT_IMAGE: {e}")

# 2) Detect & crop subject
crop = yolo_crop_subject(img_user, conf=0.35)
if crop is None:
    raise RuntimeError("YOLO không phát hiện chó/mèo/người trong ảnh. Hãy thử ảnh khác rõ chủ thể hơn.")

# 3) Xử lý sản phẩm & ảnh user (chủ yếu để log, chưa dùng vào model)
prod_rgba = remove_bg(img_prod)      # RGBA (cho tương lai, ví dụ on-ground)
crop_std  = center_pad_resize(crop, OUT_SIZE)

# 4) Tạo Canny từ crop (đưa vào ControlNet)
cond = canny_from_pil(crop_std, low=100, high=200)

show_row("Tiền xử lý", [
    ("Ảnh người dùng (crop)", crop),
    ("Sản phẩm RGBA", prod_rgba.convert("RGB")),
    ("Canny từ crop", cond),
])

# 5) Prompt tiếng Anh – bám chặt vào DOG + PRODUCT
prompt = (
    f"{BASE_SUBJECT}, wearing or interacting with a {PRODUCT_NAME_EN}. "
    f"{STYLE_HINT_EN}"
)

print("Prompt:", prompt)
print("Negative:", NEGATIVE)

# 6) Gọi ControlNet pipeline
generator = torch.Generator(device=device).manual_seed(SEED)

result = pipe(
    prompt=prompt,
    negative_prompt=NEGATIVE,
    image=cond,                        # condition = Canny
    num_inference_steps=STEPS,
    guidance_scale=GUIDANCE,
    generator=generator,
    controlnet_conditioning_scale=CTRL_SCALE,  # quan trọng
)

out_img = result.images[0]

# 7) Hiển thị kết quả
show_row("Kết quả sinh ảnh", [
    ("Ảnh generative (SD1.5 + ControlNet Canny)", out_img)
])

# Lưu nếu cần
out_path = WORK / "tryon_result.png"
out_img.save(out_path, quality=95)
print("Saved:", out_path)


# %% [code] {"execution":{"iopub.status.busy":"2025-11-27T06:20:18.418650Z","iopub.execute_input":"2025-11-27T06:20:18.419503Z","iopub.status.idle":"2025-11-27T06:20:31.053628Z","shell.execute_reply.started":"2025-11-27T06:20:18.419473Z","shell.execute_reply":"2025-11-27T06:20:31.052872Z"}}
# ===== Cell 5: Inference (phiên bản generative img2img, không ép ControlNet) =====
from diffusers import StableDiffusionImg2ImgPipeline

# 0) Khởi tạo pipeline img2img (dùng chung BASE_MODEL, dtype, device đã cấu hình ở trên)
try:
    pipe_i2i  # nếu đã tạo rồi thì dùng lại, tránh tải nhiều lần
except NameError:
    pipe_i2i = StableDiffusionImg2ImgPipeline.from_pretrained(
        BASE_MODEL,
        torch_dtype=dtype,
        safety_checker=None
    )
    if device == "cuda":
        pipe_i2i = pipe_i2i.to(device)
    pipe_i2i.enable_vae_tiling()
    print("Img2Img pipeline loaded ✔")

# 1) Load ảnh
try:
    img_user = load_image(USER_IMAGE)
except Exception as e:
    raise RuntimeError(f"Không tải được USER_IMAGE: {e}")

try:
    img_prod = load_image(PRODUCT_IMAGE)
except Exception as e:
    raise RuntimeError(f"Không tải được PRODUCT_IMAGE: {e}")

# 2) Detect & crop subject
crop = yolo_crop_subject(img_user, conf=0.35)
if crop is None:
    raise RuntimeError(
        "YOLO không phát hiện chó/mèo/người trong ảnh. "
        "Hãy thử ảnh khác rõ chủ thể hơn."
    )

# 3) Làm sạch sản phẩm (xóa nền) + chuẩn hóa kích thước crop
prod_rgba = remove_bg(img_prod)          # dùng để hiển thị cho user xem
crop_std  = center_pad_resize(crop, OUT_SIZE)

# 4) Tạo Canny từ crop chỉ để quan sát (KHÔNG đưa vào model nữa)
cond = canny_from_pil(crop_std, low=100, high=200)

show_row("Tiền xử lý", [
    ("Ảnh người dùng (crop)", crop),
    ("Canny từ crop (chỉ để tham khảo)", cond),
    ("Sản phẩm RGBA", prod_rgba.convert("RGB"))
])

# 5) Prompt: mô tả cảnh GENERATIVE – chó + sản phẩm trên nền
#    => vì là đồ đặt dưới đất (bát ăn, chuông huấn luyện, vv.) nên mô tả “đặt trước mặt”
prompt = (
    "một chú chó dễ thương đang tương tác với "
    f"{PRODUCT_NAME} đặt trên mặt đất phía trước nó, "
    "khung cảnh sáng, rõ nét, tập trung vào chó và sản phẩm, "
    f"{STYLE_HINT}"
)

# Bạn có thể chỉnh thêm NEGATIVE nếu muốn ảnh sạch hơn
negative_prompt = NEGATIVE + ", distorted, deformed dog, cut off body"

# 6) Gọi img2img – dùng crop_std làm init image
generator = torch.Generator(device=device).manual_seed(SEED)

result = pipe_i2i(
    prompt=prompt,
    negative_prompt=negative_prompt,
    image=crop_std,              # ảnh gốc của user (crop)
    strength=0.7,                # 0.4 → gần ảnh gốc, 0.8 → vẽ mới nhiều hơn
    guidance_scale=GUIDANCE,
    num_inference_steps=STEPS,
    generator=generator,
)

out_img = result.images[0]

# 7) Hiển thị kết quả
show_row("Kết quả sinh ảnh (img2img)", [
    ("Ảnh generative (SD1.5 img2img)", out_img)
])

# Lưu nếu cần
out_path = WORK / "tryon_result.png"
out_img.save(out_path, quality=95)
print("Saved:", out_path)


# %% [code] {"execution":{"iopub.status.busy":"2025-11-27T06:13:02.007135Z","iopub.execute_input":"2025-11-27T06:13:02.007383Z","iopub.status.idle":"2025-11-27T06:14:05.161818Z","shell.execute_reply.started":"2025-11-27T06:13:02.007364Z","shell.execute_reply":"2025-11-27T06:14:05.161031Z"}}
# ===== Cell 5: Inference =====
# 1) Load ảnh
try:
    img_user = load_image(USER_IMAGE)
except Exception as e:
    raise RuntimeError(f"Không tải được USER_IMAGE: {e}")

try:
    img_prod = load_image(PRODUCT_IMAGE)
except Exception as e:
    raise RuntimeError(f"Không tải được PRODUCT_IMAGE: {e}")

# 2) Detect & crop subject
crop = yolo_crop_subject(img_user, conf=0.35)
if crop is None:
    raise RuntimeError("YOLO không phát hiện chó/mèo/người trong ảnh. Hãy thử ảnh khác rõ chủ thể hơn.")

# 3) Làm sạch sản phẩm (xóa nền), chuẩn hóa kích thước
prod_rgba = remove_bg(img_prod)  # RGBA
crop_std  = center_pad_resize(crop, OUT_SIZE)

# 4) Tạo Canny từ crop (là điều kiện cho ControlNet)
cond = canny_from_pil(crop_std, low=100, high=200)

show_row("Tiền xử lý", [
    ("Ảnh người dùng (crop)", crop),
    ("Canny từ crop", cond),
    ("Sản phẩm RGBA", prod_rgba.convert("RGB"))
])

# 5) Prompt: miêu tả chủ thể + sản phẩm + style cute
prompt = (
    f"chú chó đeo {PRODUCT_NAME}, nhìn thẳng camera, "
    f"biểu cảm dễ thương, {STYLE_HINT}"
)

# 6) Gọi ControlNet pipeline
generator = torch.Generator(device=device).manual_seed(SEED)
result = pipe(
    prompt=prompt,
    negative_prompt=NEGATIVE,
    image=cond,                # ảnh điều kiện (Canny)
    num_inference_steps=STEPS,
    guidance_scale=GUIDANCE,
    generator=generator
)

out_img = result.images[0]

# 7) Hiển thị kết quả
show_row("Kết quả sinh ảnh", [
    ("Ảnh sinh ra (ControlNet-Canny + SD1.5)", out_img)
])

# Lưu nếu cần
out_path = WORK / "tryon_result.png"
out_img.save(out_path, quality=95)
print("Saved:", out_path)


# %% [code] {"execution":{"iopub.status.busy":"2025-11-27T06:14:27.992060Z","iopub.status.idle":"2025-11-27T06:14:27.992295Z","shell.execute_reply.started":"2025-11-27T06:14:27.992179Z","shell.execute_reply":"2025-11-27T06:14:27.992190Z"}}
# ===== Cell 6: Biến thể (tuỳ chọn) =====
variants = []
for sd in [7, 21, 42, 77]:
    gen = torch.Generator(device=device).manual_seed(sd)
    imgv = pipe(
        prompt=prompt,
        negative_prompt=NEGATIVE,
        image=cond,
        num_inference_steps=STEPS,
        guidance_scale=GUIDANCE,
        generator=gen
    ).images[0]
    variants.append((f"seed={sd}", imgv))

show_row("Biến thể", variants)
