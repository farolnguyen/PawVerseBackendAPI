#!/usr/bin/env python3
"""
PawVerse Breed Detection Service
Detects pet breed from uploaded image using YOLO11 + OpenCLIP + FAISS.
"""

import argparse
import json
import sys
from pathlib import Path
import time

import numpy as np
import torch
import faiss
import open_clip
from PIL import Image
from ultralytics import YOLO


# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

def clean_breed_name(raw_breed: str) -> str:
    """
    Clean breed name from Stanford format to human-readable.
    Examples: 'n02085620-Chihuahua' -> 'Chihuahua'
              'n02086240-Shih-Tzu' -> 'Shih Tzu'
    """
    if not raw_breed or raw_breed == "UNKNOWN":
        return "Unknown"
    
    # Remove prefix (e.g., 'n02085620-')
    if '-' in raw_breed:
        breed = raw_breed.split('-', 1)[1]
    else:
        breed = raw_breed
    
    # Replace hyphens with spaces
    breed = breed.replace('-', ' ')
    
    # Capitalize each word
    breed = ' '.join(word.capitalize() for word in breed.split())
    
    return breed


# ============================================================================
# CONFIGURATION
# ============================================================================

class Config:
    """Configuration singleton."""
    _instance = None
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
            cls._instance._initialize()
        return cls._instance
    
    def _initialize(self):
        # Paths
        self.BASE_DIR = Path(__file__).parent.parent
        self.MODELS_DIR = self.BASE_DIR / "Python" / "models"
        self.DATA_DIR = self.BASE_DIR / "Services" / "DetectBreed"
        
        # YOLO config
        self.YOLO_WEIGHTS = str(self.MODELS_DIR / "yolo11n.pt")
        self.YOLO_CONF = 0.25
        self.YOLO_IOU = 0.45
        self.YOLO_CLASSES = [15, 16]  # 15=cat, 16=dog in COCO
        
        # OpenCLIP config
        self.CLIP_MODEL = "ViT-B-16"
        self.CLIP_PRETRAIN = "dfn2b"
        
        # Device config
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        self.use_fp16 = torch.cuda.is_available()  # FP16 only on GPU
        
        # Create models dir
        self.MODELS_DIR.mkdir(parents=True, exist_ok=True)


# ============================================================================
# MODEL MANAGER (Singleton - Load once at startup)
# ============================================================================

class ModelManager:
    """Manages ML models - loads once and reuses."""
    _instance = None
    _initialized = False
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
        return cls._instance
    
    def __init__(self):
        if not ModelManager._initialized:
            self.config = Config()
            self.yolo = None
            self.clip_model = None
            self.preprocess = None
            self.faiss_indices = {}
            self._load_models()
            ModelManager._initialized = True
    
    def _load_models(self):
        """Load YOLO and OpenCLIP models."""
        print("[ModelManager] Loading models...", file=sys.stderr)
        start = time.time()
        
        # Load YOLO
        self.yolo = YOLO(self.config.YOLO_WEIGHTS)
        print(f"[ModelManager] YOLO loaded", file=sys.stderr)
        
        # Load OpenCLIP
        self.clip_model, _, self.preprocess = open_clip.create_model_and_transforms(
            self.config.CLIP_MODEL,
            pretrained=self.config.CLIP_PRETRAIN,
            device=self.config.device
        )
        self.clip_model.eval()
        
        # Convert to FP16 for speed optimization on GPU
        if self.config.use_fp16:
            self.clip_model = self.clip_model.half()
            print(f"[ModelManager] OpenCLIP loaded with FP16 on {self.config.device}", file=sys.stderr)
        else:
            print(f"[ModelManager] OpenCLIP loaded on {self.config.device}", file=sys.stderr)
        
        # Clear GPU cache
        if torch.cuda.is_available():
            torch.cuda.empty_cache()
        
        elapsed = time.time() - start
        print(f"[ModelManager] All models loaded in {elapsed:.2f}s", file=sys.stderr)
    
    def load_faiss_index(self, animal_type: str):
        """Load FAISS index and id_map for animal type (dog/cat)."""
        if animal_type in self.faiss_indices:
            return self.faiss_indices[animal_type]
        
        data_path = self.config.DATA_DIR / animal_type
        faiss_path = data_path / "faiss_IndexFlatIP.faiss"
        idmap_path = data_path / "id_map.json"
        
        if not faiss_path.exists():
            raise FileNotFoundError(f"FAISS index not found: {faiss_path}")
        if not idmap_path.exists():
            raise FileNotFoundError(f"ID map not found: {idmap_path}")
        
        # Load FAISS index
        index = faiss.read_index(str(faiss_path))
        
        # Load id_map
        with open(idmap_path, 'r', encoding='utf-8') as f:
            id_map = json.load(f)
        
        self.faiss_indices[animal_type] = (index, id_map)
        print(f"[ModelManager] Loaded {animal_type} database: {index.ntotal} vectors", file=sys.stderr)
        
        return index, id_map


# ============================================================================
# DETECTION PIPELINE
# ============================================================================

class BreedDetector:
    """Main detection pipeline."""
    
    def __init__(self):
        self.models = ModelManager()
        self.config = Config()
    
    def detect_animal(self, image_path: str):
        """Detect dog/cat using YOLO. Returns (bbox, confidence, class) or (None, None, None)."""
        results = self.models.yolo.predict(
            source=image_path,
            conf=self.config.YOLO_CONF,
            iou=self.config.YOLO_IOU,
            classes=self.config.YOLO_CLASSES,
            verbose=False
        )
        
        r = results[0]
        if r.boxes is None or r.boxes.shape[0] == 0:
            return None, None, None
        
        # Get highest confidence detection
        confs = r.boxes.conf.cpu().numpy()
        classes = r.boxes.cls.cpu().numpy()
        i_best = int(np.argmax(confs))
        
        bbox = r.boxes.xyxy.cpu().numpy().astype(int)[i_best]
        conf = float(confs[i_best])
        animal_class = int(classes[i_best])
        
        return bbox.tolist(), conf, animal_class
    
    def crop_image(self, image_path: str, bbox: list, pad: int = 2):
        """Crop image with padding around bounding box."""
        img = Image.open(image_path).convert('RGB')
        
        if bbox is None:
            return img
        
        x1, y1, x2, y2 = bbox
        x1 = max(0, x1 - pad)
        y1 = max(0, y1 - pad)
        x2 = min(img.width, x2 + pad)
        y2 = min(img.height, y2 + pad)
        
        if x2 <= x1 or y2 <= y1:
            return img
        
        return img.crop((x1, y1, x2, y2))
    
    def embed_image(self, image: Image.Image):
        """Embed image using OpenCLIP."""
        with torch.no_grad():
            img_tensor = self.models.preprocess(image).unsqueeze(0).to(self.config.device)
            
            # Convert to FP16 if enabled
            if self.config.use_fp16:
                img_tensor = img_tensor.half()
            
            features = self.models.clip_model.encode_image(img_tensor)
            features = torch.nn.functional.normalize(features, dim=-1)
        
        return features.cpu().float().numpy()  # Back to FP32 for FAISS
    
    def search_faiss(self, vector: np.ndarray, animal_type: str, top_k: int = 10):
        """Search in FAISS index."""
        index, id_map = self.models.load_faiss_index(animal_type)
        similarities, indices = index.search(vector, top_k)
        return similarities[0].tolist(), indices[0].tolist(), id_map
    
    def vote_breed(self, similarities: list, indices: list, id_map: list):
        """Weighted voting to determine best breed."""
        breed_scores = {}
        
        for sim, idx in zip(similarities, indices):
            # Get breed from id_map
            if isinstance(id_map, list):
                breed = id_map[idx].get('breed', 'UNKNOWN')
            else:  # dict
                breed = id_map.get(str(idx), {}).get('breed', 'UNKNOWN')
            
            # Accumulate similarity scores (use max score for each breed)
            if breed not in breed_scores:
                breed_scores[breed] = float(sim)
            else:
                breed_scores[breed] = max(breed_scores[breed], float(sim))
        
        if not breed_scores:
            return "UNKNOWN", 0.0
        
        # Get best breed
        best_breed = max(breed_scores.items(), key=lambda x: x[1])
        
        return best_breed[0], best_breed[1]
    
    def get_top_breeds(self, similarities: list, indices: list, id_map: list, top_k: int = 5):
        """Get top K breed candidates with aggregated scores."""
        breed_scores = {}
        
        # Aggregate scores - use MAX score for each unique breed
        for sim, idx in zip(similarities, indices):
            if isinstance(id_map, list):
                breed = id_map[idx].get('breed', 'UNKNOWN')
            else:
                breed = id_map.get(str(idx), {}).get('breed', 'UNKNOWN')
            
            if breed not in breed_scores:
                breed_scores[breed] = float(sim)
            else:
                breed_scores[breed] = max(breed_scores[breed], float(sim))
        
        # Sort by score descending
        sorted_breeds = sorted(breed_scores.items(), key=lambda x: x[1], reverse=True)
        
        # Return top K
        top_breeds = []
        for rank, (breed, score) in enumerate(sorted_breeds[:top_k], start=1):
            top_breeds.append({
                "breed": clean_breed_name(breed),
                "breed_raw": breed,
                "score": round(score, 3),
                "rank": rank
            })
        
        return top_breeds
    
    def detect_breed(self, image_path: str, animal_type: str = "dog"):
        """Main detection pipeline."""
        start_time = time.time()
        
        try:
            # Step 1: Detect animal
            bbox, det_conf, animal_class = self.detect_animal(image_path)
            
            if bbox is None:
                return {
                    "success": False,
                    "error": "No pet detected in image. Please upload a clearer photo with the pet visible.",
                    "animal_detected": False
                }
            
            # Determine animal type from YOLO class
            detected_type = "cat" if animal_class == 15 else "dog"
            
            # Step 2: Crop image
            crop = self.crop_image(image_path, bbox)
            
            # Step 3: Embed
            vector = self.embed_image(crop)
            
            # Step 4: Search FAISS (search more to get better aggregation)
            sims, idxs, id_map = self.search_faiss(vector, detected_type, top_k=50)
            
            # Step 5: Get top K breed candidates
            top_breeds = self.get_top_breeds(sims, idxs, id_map, top_k=5)
            
            # Step 6: Get best breed (first in top_breeds)
            if top_breeds:
                best = top_breeds[0]
                breed_clean = best["breed"]
                best_breed_raw = best["breed_raw"]
                confidence = best["score"]
            else:
                breed_clean = "Unknown"
                best_breed_raw = "Unknown"
                confidence = 0.0
            
            # Calculate processing time
            process_time = int((time.time() - start_time) * 1000)
            
            return {
                "success": True,
                "breed": breed_clean,
                "breed_raw": best_breed_raw,
                "confidence": round(confidence, 3),
                "animal_type": detected_type,
                "top_breeds": top_breeds,
                "metadata": {
                    "animal_detected": True,
                    "detection_confidence": round(det_conf, 3),
                    "bounding_box": bbox,
                    "processing_time_ms": process_time
                }
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": f"Processing error: {str(e)}",
                "animal_detected": False
            }


# ============================================================================
# CLI INTERFACE
# ============================================================================

def main():
    parser = argparse.ArgumentParser(description='PawVerse Breed Detection')
    parser.add_argument('--image', help='Path to image file')
    parser.add_argument('--type', default='dog', choices=['dog', 'cat'], help='Animal type')
    parser.add_argument('--init-only', action='store_true', help='Only initialize models')
    
    args = parser.parse_args()
    
    # Initialize detector (loads models)
    detector = BreedDetector()
    
    if args.init_only:
        print(json.dumps({"success": True, "message": "Models initialized"}))
        return
    
    if not args.image:
        print(json.dumps({"success": False, "error": "--image argument required"}))
        return
    
    # Run detection
    result = detector.detect_breed(args.image, args.type)
    
    # Output JSON to stdout
    print(json.dumps(result, ensure_ascii=False))


if __name__ == '__main__':
    main()
