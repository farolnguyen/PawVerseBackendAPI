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
        
        # Fix PyTorch 2.6+ weights_only issue for YOLO
        self._fix_torch_load()
        
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        print(f"📱 Device: {self.device}")
        
        # Load metadata
        self.metadata = self._load_metadata()
        
        # Load models
        self.yolo_model = self._load_yolo()
        self.sd_pipeline = self._load_stable_diffusion()
        
        print("✅ Pipeline initialized!")
    
    def _fix_torch_load(self):
        """Fix PyTorch 2.6+ weights_only=True default for YOLO models"""
        try:
            import torch
            # Method 1: Add safe globals
            if hasattr(torch.serialization, 'add_safe_globals'):
                torch.serialization.add_safe_globals(['DetectionModel', 'ultralytics.nn.tasks.DetectionModel'])
            
            # Method 2: Monkey-patch torch.load
            original_load = torch.load
            def patched_load(*args, **kwargs):
                # Force weights_only=False for .pt files (YOLO models)
                if 'weights_only' not in kwargs:
                    kwargs['weights_only'] = False
                return original_load(*args, **kwargs)
            torch.load = patched_load
            
            print("✅ PyTorch load patched for YOLO compatibility")
        except Exception as e:
            print(f"⚠️ Could not patch torch.load: {e}")
    
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
        """Load YOLO11 model (torch.load already patched)"""
        try:
            from ultralytics import YOLO
            
            print("📥 Loading YOLO11n model (will auto-download if needed)...")
            
            # Load model - torch.load is already patched for compatibility
            model = YOLO('yolo11n.pt')
            
            print("✅ YOLO11 loaded successfully")
            return model
            
        except Exception as e:
            print(f"❌ Failed to load YOLO: {e}")
            print("\n💡 If download fails, run in a cell:")
            print("!wget https://github.com/ultralytics/assets/releases/download/v8.3.0/yolo11n.pt")
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
            
            # Memory optimizations (safe methods only)
            if self.device == "cuda":
                # Use attention slicing (safe, no Flash-Attention needed)
                pipe.enable_attention_slicing(1)
                print("✅ Attention slicing enabled")
                
                # Try xformers only if available (don't force install)
                try:
                    pipe.enable_xformers_memory_efficient_attention()
                    print("✅ xformers enabled")
                except Exception as e:
                    print(f"⚠️ xformers not available (using standard attention)")
            
            print("✅ Stable Diffusion + ControlNet loaded")
            return pipe
            
        except Exception as e:
            print(f"❌ Failed to load SD: {e}")
            raise
    
    def detect_animal(self, image):
        """
        Detect and crop animal from image
        
        Args:
            image: PIL Image
            
        Returns:
            dict with detection results
        """
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
        """
        Generate Canny edge detection
        
        Args:
            image: PIL Image
            low_threshold: Lower threshold for Canny
            high_threshold: Upper threshold for Canny
            
        Returns:
            PIL Image (grayscale edge map)
        """
        # Convert to grayscale
        img_np = np.array(image)
        gray = cv2.cvtColor(img_np, cv2.COLOR_RGB2GRAY)
        
        # Apply Canny
        edges = cv2.Canny(gray, low_threshold, high_threshold)
        
        # Convert back to PIL
        edges_pil = Image.fromarray(edges)
        
        return edges_pil
    
    def build_prompt(self, product_id, style_id, animal_type):
        """
        Build prompt from metadata
        
        Args:
            product_id: Product identifier
            style_id: Style preset
            animal_type: 'dog' or 'cat'
            
        Returns:
            dict with 'positive' and 'negative' prompts
        """
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
        """
        Generate try-on image
        
        Args:
            image: PIL Image (pet photo)
            product_id: Product identifier
            style_id: Style preset
            animal_type: Override detected type
            progress_callback: Optional callback for progress (0-1)
            
        Returns:
            dict with result image and metadata
        """
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
        
        # Prepare callback
        def step_callback(step, timestep, latents):
            if progress_callback:
                progress_callback(0.3 + (step / 20) * 0.7)
        
        with torch.autocast(self.device):
            result = self.sd_pipeline(
                prompt=prompts['positive'],
                negative_prompt=prompts['negative'],
                image=canny_image,
                num_inference_steps=20,  # Reduced for speed
                guidance_scale=7.5,
                controlnet_conditioning_scale=0.7,
                callback=step_callback if progress_callback else None,
                callback_steps=1  # Required for diffusers >= 0.27.0
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
