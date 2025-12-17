# 🔧 Fixed Kaggle Notebook Cells

This document contains the corrected cells for the Kaggle notebook to fix the background process error.

---

## 📋 **COPY THESE CELLS TO YOUR KAGGLE NOTEBOOK**

Replace the problematic cells with these fixed versions.

---

### **Cell 1: Install Dependencies**

```python
%%time
# Install required packages
!pip install -q diffusers transformers accelerate xformers
!pip install -q ultralytics opencv-python streamlit pyngrok

print("✅ All packages installed!")
```

---

### **Cell 2: Check Environment**

```python
import torch
import sys
from pathlib import Path

print("🐍 Python version:", sys.version)
print("🔥 PyTorch version:", torch.__version__)
print("🎮 CUDA available:", torch.cuda.is_available())

if torch.cuda.is_available():
    print("📱 GPU:", torch.cuda.get_device_name(0))
    print("💾 GPU Memory:", torch.cuda.get_device_properties(0).total_memory / 1024**3, "GB")
else:
    print("⚠️ No GPU detected! This will be very slow.")

# Check datasets
print("\n📂 Checking datasets...")
metadata_path = Path("/kaggle/input/tryon-metadata/tryon_metadata.json")
products_path = Path("/kaggle/input/tryon-products/datatryon")

print(f"  Metadata: {'✅' if metadata_path.exists() else '❌'} {metadata_path}")
print(f"  Products: {'✅' if products_path.exists() else '❌'} {products_path}")

if products_path.exists():
    product_files = list(products_path.glob("*.png"))
    print(f"  Found {len(product_files)} product images")
```

---

### **Cell 3: Create inference_pipeline.py**

```python
%%writefile inference_pipeline.py
"""
Inference Pipeline for PawVerse AI Try-On
Handles YOLO detection + Canny + Stable Diffusion + ControlNet
"""

import torch
import numpy as np
from PIL import Image
import cv2
import json
import time
from pathlib import Path

class TryOnPipeline:
    """Complete pipeline for pet try-on generation"""
    
    def __init__(self):
        """Initialize models"""
        print("🔄 Initializing Try-On Pipeline...")
        
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        print(f"📱 Device: {self.device}")
        
        # Load metadata
        self.metadata = self._load_metadata()
        
        # Load models
        self.yolo_model = self._load_yolo()
        self.sd_pipeline = self._load_stable_diffusion()
        
        print("✅ Pipeline initialized!")
    
    def _load_metadata(self):
        """Load product metadata"""
        try:
            with open('/kaggle/input/tryon-metadata/tryon_metadata.json', 'r') as f:
                metadata = json.load(f)
            print(f"✅ Loaded metadata for {len(metadata['products'])} products")
            return metadata
        except Exception as e:
            print(f"❌ Failed to load metadata: {e}")
            return None
    
    def _load_yolo(self):
        """Load YOLO11 model"""
        try:
            from ultralytics import YOLO
            model = YOLO('yolo11n.pt')  # Will auto-download
            print("✅ YOLO11 loaded")
            return model
        except Exception as e:
            print(f"❌ Failed to load YOLO: {e}")
            raise
    
    def _load_stable_diffusion(self):
        """Load Stable Diffusion + ControlNet"""
        try:
            from diffusers import StableDiffusionControlNetPipeline, ControlNetModel
            from diffusers import UniPCMultistepScheduler
            
            # Load ControlNet
            print("⏳ Loading ControlNet Canny...")
            controlnet = ControlNetModel.from_pretrained(
                "lllyasviel/sd-controlnet-canny",
                torch_dtype=torch.float16
            )
            
            # Load SD pipeline
            print("⏳ Loading Stable Diffusion 1.5...")
            pipe = StableDiffusionControlNetPipeline.from_pretrained(
                "runwayml/stable-diffusion-v1-5",
                controlnet=controlnet,
                torch_dtype=torch.float16,
                safety_checker=None
            )
            
            # Optimizations
            pipe.scheduler = UniPCMultistepScheduler.from_config(pipe.scheduler.config)
            pipe = pipe.to(self.device)
            
            # Memory optimizations
            if self.device == "cuda":
                try:
                    pipe.enable_xformers_memory_efficient_attention()
                    print("✅ xformers enabled")
                except:
                    print("⚠️ xformers not available")
                
                pipe.enable_attention_slicing(1)
                print("✅ Attention slicing enabled")
            
            print("✅ Stable Diffusion + ControlNet loaded")
            return pipe
            
        except Exception as e:
            print(f"❌ Failed to load SD: {e}")
            raise
    
    def detect_animal(self, image):
        """Detect and crop animal from image"""
        # Convert to numpy
        img_np = np.array(image)
        
        # Run YOLO
        results = self.yolo_model.predict(img_np, verbose=False)
        
        # Parse results
        if len(results[0].boxes) == 0:
            return {
                'detected': False,
                'animal_type': None,
                'confidence': 0,
                'bbox': None
            }
        
        # Get first detection
        box = results[0].boxes[0]
        class_id = int(box.cls[0])
        confidence = float(box.conf[0])
        
        # YOLO COCO classes: 15=cat, 16=dog
        animal_type = 'cat' if class_id == 15 else ('dog' if class_id == 16 else None)
        
        if animal_type is None:
            return {
                'detected': False,
                'animal_type': None,
                'confidence': 0,
                'bbox': None
            }
        
        # Get bounding box
        bbox = box.xyxy[0].cpu().numpy()
        
        return {
            'detected': True,
            'animal_type': animal_type,
            'confidence': confidence,
            'bbox': bbox.tolist()
        }
    
    def generate_canny(self, image, low_threshold=100, high_threshold=200):
        """Generate Canny edge detection"""
        # Convert to grayscale
        img_np = np.array(image)
        gray = cv2.cvtColor(img_np, cv2.COLOR_RGB2GRAY)
        
        # Apply Canny
        edges = cv2.Canny(gray, low_threshold, high_threshold)
        
        # Convert back to PIL
        edges_pil = Image.fromarray(edges)
        
        return edges_pil
    
    def build_prompt(self, product_id, style_id, animal_type):
        """Build prompt from metadata"""
        # Find product
        product = next(
            (p for p in self.metadata['products'] if p['product_id'] == product_id),
            None
        )
        
        if product is None:
            raise ValueError(f"Product {product_id} not found")
        
        # Get base prompt
        base_prompt = product['prompt_engineering']['detailed_prompt'].format(
            animal_type=animal_type
        )
        
        # Style modifiers
        styles = {
            'chibi': 'chibi anime style, kawaii aesthetic, big sparkling eyes, tiny body proportions, pastel colors, adorable, cute',
            'anime': 'anime style, vibrant colors, clean lines, cel shaded, expressive, detailed',
            'cartoon': 'cartoon illustration style, bold outlines, bright colors, playful, simple shapes, fun'
        }
        
        style_suffix = styles.get(style_id, styles['chibi'])
        
        # Combine
        full_prompt = f"{base_prompt}, {style_suffix}"
        
        # Add quality boosters
        full_prompt += ", high quality, detailed, professional, masterpiece"
        
        # Negative prompt
        negative = product['prompt_engineering']['negative_prompt']
        negative += ", low quality, blurry, distorted, ugly, bad anatomy, deformed"
        
        return {
            'positive': full_prompt,
            'negative': negative
        }
    
    def generate(self, image, product_id, style_id, animal_type=None, progress_callback=None):
        """Generate try-on image"""
        start_time = time.time()
        
        # 1. Detect animal if not provided
        if animal_type is None:
            detection = self.detect_animal(image)
            if not detection['detected']:
                raise ValueError("No animal detected in image")
            animal_type = detection['animal_type']
        
        if progress_callback:
            progress_callback(0.2)
        
        # 2. Resize image
        image = self._resize_image(image, target_size=512)
        
        # 3. Generate Canny edge
        canny_image = self.generate_canny(image)
        
        if progress_callback:
            progress_callback(0.3)
        
        # 4. Build prompt
        prompts = self.build_prompt(product_id, style_id, animal_type)
        
        # 5. Generate with SD
        print(f"🎨 Generating with prompt: {prompts['positive'][:100]}...")
        
        with torch.autocast(self.device):
            result = self.sd_pipeline(
                prompt=prompts['positive'],
                negative_prompt=prompts['negative'],
                image=canny_image,
                num_inference_steps=20,
                guidance_scale=7.5,
                controlnet_conditioning_scale=0.7,
                callback=lambda step, *args: progress_callback((0.3 + (step / 20) * 0.7)) if progress_callback else None
            )
        
        # Get result
        result_image = result.images[0]
        
        # Calculate time
        processing_time = time.time() - start_time
        
        return {
            'image': result_image,
            'animal_type': animal_type,
            'product_id': product_id,
            'style_id': style_id,
            'processing_time': processing_time,
            'prompt': prompts['positive'],
            'negative_prompt': prompts['negative']
        }
    
    def _resize_image(self, image, target_size=512):
        """Resize image to target size while maintaining aspect ratio"""
        # Calculate new size
        width, height = image.size
        
        if width > height:
            new_width = target_size
            new_height = int(height * (target_size / width))
        else:
            new_height = target_size
            new_width = int(width * (target_size / height))
        
        # Resize
        image = image.resize((new_width, new_height), Image.LANCZOS)
        
        # Pad to square
        canvas = Image.new('RGB', (target_size, target_size), (255, 255, 255))
        offset = ((target_size - new_width) // 2, (target_size - new_height) // 2)
        canvas.paste(image, offset)
        
        return canvas
```

---

### **Cell 4: Create tryon_app.py** (Part 1)

```python
%%writefile tryon_app.py
"""
PawVerse AI Try-On - Streamlit Demo
Demo for virtual product try-on for pets
"""

import streamlit as st
from PIL import Image
import numpy as np
import json
import torch
from pathlib import Path
import time

# Page config
st.set_page_config(
    page_title="🐾 PawVerse AI Try-On",
    page_icon="🐾",
    layout="wide",
    initial_sidebar_state="expanded"
)

# Custom CSS
st.markdown("""
<style>
    .main-header {
        font-size: 3rem;
        font-weight: bold;
        color: #FF6B35;
        text-align: center;
        margin-bottom: 1rem;
    }
    .sub-header {
        font-size: 1.2rem;
        text-align: center;
        color: #666;
        margin-bottom: 2rem;
    }
    .stButton>button {
        background-color: #FF6B35;
        color: white;
        font-size: 1.2rem;
        padding: 0.5rem 2rem;
        border-radius: 10px;
        border: none;
        width: 100%;
    }
    .stButton>button:hover {
        background-color: #E55A2B;
    }
</style>
""", unsafe_allow_html=True)

# Title
st.markdown('<div class="main-header">🐾 PawVerse AI Try-On</div>', unsafe_allow_html=True)
st.markdown('<div class="sub-header">Virtual Product Try-On for Your Pets using AI ✨</div>', unsafe_allow_html=True)

# Initialize session state
if 'pipeline' not in st.session_state:
    st.session_state.pipeline = None
    st.session_state.models_loaded = False
    st.session_state.result_image = None
    st.session_state.metadata = None

# Load metadata
@st.cache_data
def load_metadata():
    """Load product metadata"""
    try:
        with open('/kaggle/input/tryon-metadata/tryon_metadata.json', 'r') as f:
            return json.load(f)
    except FileNotFoundError:
        st.error("❌ Metadata file not found.")
        return None

# Load models (cached)
@st.cache_resource
def load_pipeline():
    """Load AI models - runs once and cached"""
    with st.spinner("🔄 Loading AI models... (1-2 minutes on first run)"):
        try:
            from inference_pipeline import TryOnPipeline
            pipeline = TryOnPipeline()
            return pipeline
        except Exception as e:
            st.error(f"❌ Failed to load models: {str(e)}")
            return None

# Sidebar
st.sidebar.title("⚙️ Settings")

# Load metadata
metadata = load_metadata()

if metadata is None:
    st.stop()

# Style selection
st.sidebar.subheader("🎨 Choose Style")
styles = {
    "chibi": {"name": "Chibi Anime 🎀", "desc": "Cute anime style with big eyes"},
    "anime": {"name": "Anime Style 🌸", "desc": "Japanese animation style"},
    "cartoon": {"name": "Cartoon 🎪", "desc": "Playful cartoon illustration"}
}

selected_style = st.sidebar.radio(
    "Select your preferred style:",
    options=list(styles.keys()),
    format_func=lambda x: styles[x]["name"]
)

st.sidebar.info(styles[selected_style]["desc"])

# Product selection
st.sidebar.subheader("🛍️ Choose Product")

animal_filter = st.sidebar.selectbox(
    "Filter by pet type:",
    options=["all", "dog", "cat"],
    format_func=lambda x: "All Pets 🐾" if x == "all" else ("Dogs 🐕" if x == "dog" else "Cats 🐱")
)

products = metadata['products']
if animal_filter != "all":
    products = [p for p in products if animal_filter in p['compatible_animals']]

product_options = {p['product_id']: p['name_en'] for p in products}
selected_product_id = st.sidebar.selectbox(
    "Select product:",
    options=list(product_options.keys()),
    format_func=lambda x: product_options[x]
)

selected_product = next(p for p in products if p['product_id'] == selected_product_id)

st.sidebar.divider()
st.sidebar.subheader("📦 Product Details")
st.sidebar.write(f"**Name:** {selected_product['name_en']}")
st.sidebar.write(f"**Category:** {selected_product['category']}")
st.sidebar.write(f"**Compatible:** {', '.join(selected_product['compatible_animals'])}")

try:
    product_img_path = f"/kaggle/input/tryon-products/datatryon/{selected_product['source_file']}"
    product_img = Image.open(product_img_path)
    st.sidebar.image(product_img, caption=selected_product['name_en'], use_column_width=True)
except:
    st.sidebar.warning("📷 Product image not available")

# Main area
col1, col2 = st.columns([1, 1])

with col1:
    st.subheader("📤 Upload Pet Photo")
    
    uploaded_file = st.file_uploader(
        "Choose an image of your pet",
        type=['jpg', 'jpeg', 'png'],
        help="Upload a clear photo of your dog or cat"
    )
    
    if uploaded_file is not None:
        input_image = Image.open(uploaded_file)
        st.image(input_image, caption="Your Pet Photo", use_column_width=True)
        st.caption(f"📏 Size: {input_image.size[0]}x{input_image.size[1]}px")
    else:
        st.info("👆 Please upload a photo of your pet")
        st.markdown("### 💡 Tips:")
        st.markdown("""
        - ✅ Clear, well-lit photo
        - ✅ Pet clearly visible
        - ✅ Single pet in frame
        """)

with col2:
    st.subheader("✨ Try-On Result")
    
    if uploaded_file is not None:
        if st.button("🎨 Generate Try-On Image", use_container_width=True):
            if st.session_state.pipeline is None:
                st.session_state.pipeline = load_pipeline()
            
            if st.session_state.pipeline is not None:
                progress_bar = st.progress(0)
                status_text = st.empty()
                
                try:
                    status_text.text("🔍 Detecting pet...")
                    progress_bar.progress(20)
                    
                    detection_result = st.session_state.pipeline.detect_animal(input_image)
                    
                    if not detection_result['detected']:
                        st.error("❌ No pet detected. Please upload a clear photo of a dog or cat.")
                        st.stop()
                    
                    st.success(f"✅ Detected: {detection_result['animal_type'].upper()} ({detection_result['confidence']:.2%})")
                    
                    status_text.text("🎨 Generating try-on image...")
                    progress_bar.progress(40)
                    
                    result = st.session_state.pipeline.generate(
                        image=input_image,
                        product_id=selected_product_id,
                        style_id=selected_style,
                        animal_type=detection_result['animal_type'],
                        progress_callback=lambda p: progress_bar.progress(40 + int(p * 55))
                    )
                    
                    progress_bar.progress(100)
                    status_text.text("✅ Complete!")
                    time.sleep(0.5)
                    
                    st.session_state.result_image = result
                    st.image(result['image'], caption="Try-On Result", use_column_width=True)
                    st.success(f"⏱️ Generated in {result['processing_time']:.1f}s")
                    
                    from io import BytesIO
                    buf = BytesIO()
                    result['image'].save(buf, format='PNG')
                    buf.seek(0)
                    
                    st.download_button(
                        label="💾 Download Result",
                        data=buf,
                        file_name=f"tryon_{selected_product_id}_{selected_style}.png",
                        mime="image/png",
                        use_container_width=True
                    )
                    
                except Exception as e:
                    st.error(f"❌ Generation failed: {str(e)}")
        
        elif st.session_state.result_image is not None:
            st.image(st.session_state.result_image['image'], caption="Previous Result", use_column_width=True)
    else:
        st.info("👈 Upload a pet photo to generate")

# Footer
st.divider()
st.markdown("""
<div style='text-align: center; color: #666; padding: 1rem;'>
    <p>🐾 <strong>PawVerse AI Try-On</strong> | YOLO11 + SD + ControlNet</p>
</div>
""", unsafe_allow_html=True)

st.sidebar.divider()
st.sidebar.success("✅ App ready!")
```

---

### **Cell 5: Setup ngrok**

```python
# Setup ngrok for public URL
from pyngrok import ngrok, conf

# IMPORTANT: Add your ngrok token here
NGROK_TOKEN = "YOUR_NGROK_TOKEN_HERE"  # Replace with your token!

if NGROK_TOKEN != "YOUR_NGROK_TOKEN_HERE":
    conf.get_default().auth_token = NGROK_TOKEN
    print("✅ ngrok configured")
else:
    print("⚠️ Please add your ngrok token above!")
    print("Get it from: https://dashboard.ngrok.com/get-started/your-authtoken")
```

---

### **Cell 6: Start Streamlit (FIXED!)** ⭐

**Method 1: Using subprocess (Recommended)**

```python
%%time
import subprocess
import time
import os
import signal

# Kill any existing streamlit processes
print("🔄 Cleaning up existing processes...")
try:
    subprocess.run(['pkill', '-9', '-f', 'streamlit'], stderr=subprocess.DEVNULL)
    time.sleep(2)
except:
    pass

# Start streamlit using subprocess.Popen (non-blocking)
print("⏳ Starting Streamlit server...")

streamlit_process = subprocess.Popen(
    ['streamlit', 'run', 'tryon_app.py', 
     '--server.port', '8501',
     '--server.headless', 'true',
     '--server.fileWatcherType', 'none',
     '--browser.gatherUsageStats', 'false'],
    stdout=subprocess.PIPE,
    stderr=subprocess.PIPE
)

# Wait for server to start
time.sleep(10)

# Check if process is running
if streamlit_process.poll() is None:
    print("✅ Streamlit server started successfully!")
    print(f"📊 Process ID: {streamlit_process.pid}")
else:
    print("❌ Streamlit failed to start")
    stdout, stderr = streamlit_process.communicate()
    print("STDOUT:", stdout.decode())
    print("STDERR:", stderr.decode())
```

**OR Method 2: Using get_ipython() (Alternative)**

```python
%%time
import time
from IPython import get_ipython

# Kill existing processes
get_ipython().system('pkill -9 -f streamlit 2>/dev/null || true')
time.sleep(2)

# Start streamlit using system_raw (allows background)
print("⏳ Starting Streamlit server...")
get_ipython().system_raw('streamlit run tryon_app.py --server.port 8501 --server.headless true &')

# Wait for server
time.sleep(10)
print("✅ Streamlit server started!")
```

---

### **Cell 7: Create Public URL**

```python
# Create public URL with ngrok
public_url = ngrok.connect(8501, bind_tls=True)

print("\n" + "="*60)
print("🎉 STREAMLIT APP IS READY!")
print("="*60)
print(f"\n🌐 Public URL: {public_url}")
print(f"\n📱 Local URL: http://localhost:8501")
print("\n💡 Share this URL to demo the app!")
print("\n⚠️ Keep this notebook running")
print("="*60)
```

---

### **Cell 8: Keep Running**

```python
# Keep notebook alive
import time

print("🔄 Server is running...")
print("Press ⏹️ Stop button to shutdown\n")

try:
    while True:
        time.sleep(60)
        print("💚 Server active...", time.strftime("%H:%M:%S"))
except KeyboardInterrupt:
    print("\n⏹️ Shutting down...")
    # Cleanup
    try:
        ngrok.disconnect(public_url)
        streamlit_process.kill()
    except:
        pass
    print("✅ Shutdown complete")
```

---

### **Cell 9: Cleanup (Run when done)**

```python
# Stop all services
try:
    ngrok.kill()
    print("✅ ngrok stopped")
except:
    pass

try:
    streamlit_process.terminate()
    streamlit_process.wait(timeout=5)
    print("✅ Streamlit stopped")
except:
    pass

import subprocess
subprocess.run(['pkill', '-9', '-f', 'streamlit'], stderr=subprocess.DEVNULL)
print("✅ All processes cleaned up")
```

---

## ✅ **SUMMARY OF FIXES**

### **What was wrong:**
```python
!streamlit run app.py &  # ❌ Background process not supported
```

### **What's fixed:**
```python
# Method 1: subprocess.Popen
streamlit_process = subprocess.Popen([...])  # ✅ Works!

# Method 2: get_ipython().system_raw()
get_ipython().system_raw('streamlit run app.py &')  # ✅ Works!
```

---

## 🎯 **RECOMMENDED APPROACH**

Use **Method 1** (subprocess.Popen) because:
- ✅ More reliable
- ✅ Returns process object
- ✅ Can kill cleanly later
- ✅ Better error handling

---

**Copy these cells to your Kaggle notebook and you should be good to go!** 🚀
