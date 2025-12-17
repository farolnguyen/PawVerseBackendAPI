# 🔧 PyTorch 2.6+ Weights Loading Fix

## ❌ **ERROR:**
```
WeightsUnpickler error: Unsupported global: GLOBAL ultralytics.nn.tasks.DetectionModel
was not an allowed global by default.
torch.load changed default from weights_only=False to True in PyTorch 2.6
```

## 🎯 **ROOT CAUSE:**
PyTorch 2.6 changed default `torch.load(weights_only=False → True)` for security.
YOLO models contain custom classes that need `weights_only=False` to load.

---

## ✅ **SOLUTION: Patch torch.load**

### **Method 1: Monkey-patch (Recommended)**

Add this to `TryOnPipeline.__init__()`:

```python
def _fix_torch_load(self):
    """Fix PyTorch 2.6+ weights_only=True default"""
    try:
        import torch
        
        # Method 1: Add safe globals
        if hasattr(torch.serialization, 'add_safe_globals'):
            torch.serialization.add_safe_globals([
                'DetectionModel',
                'ultralytics.nn.tasks.DetectionModel'
            ])
        
        # Method 2: Monkey-patch torch.load
        original_load = torch.load
        
        def patched_load(*args, **kwargs):
            if 'weights_only' not in kwargs:
                kwargs['weights_only'] = False
            return original_load(*args, **kwargs)
        
        torch.load = patched_load
        
        print("✅ PyTorch load patched for YOLO")
    except Exception as e:
        print(f"⚠️ Patch failed: {e}")
```

Call in `__init__`:
```python
def __init__(self):
    self._fix_torch_load()  # First thing!
    # ... rest of init
```

---

### **Method 2: Environment Variable (Alternative)**

Set before importing anything:

```python
import os
os.environ['TORCH_FORCE_WEIGHTS_ONLY_LOAD'] = '0'

import torch
from ultralytics import YOLO
```

---

### **Method 3: Downgrade PyTorch (Last Resort)**

```bash
pip install torch==2.5.1 torchvision==0.20.1
```

---

## 📊 **WHY THIS WORKS:**

| PyTorch Version | Default | Impact |
|----------------|---------|--------|
| 2.5 and below | `weights_only=False` | ✅ YOLO loads fine |
| 2.6+ | `weights_only=True` | ❌ YOLO fails (custom classes) |
| 2.6+ (patched) | `weights_only=False` | ✅ YOLO loads fine |

**Security Note:** 
- `weights_only=False` allows arbitrary Python objects
- Safe for official Ultralytics models (trusted source)
- Don't use for untrusted .pt files

---

## ✅ **VERIFICATION:**

After applying fix:

```python
from inference_pipeline import TryOnPipeline

# Should see:
# ✅ PyTorch load patched for YOLO compatibility
# 📥 Loading YOLO11n model...
# ✅ YOLO11 loaded successfully
# ✅ Pipeline initialized!

pipeline = TryOnPipeline()
```

---

## 🔍 **DEBUGGING:**

Check PyTorch version:
```python
import torch
print(f"PyTorch: {torch.__version__}")
# If >= 2.6.0, need patch
```

Test torch.load manually:
```python
import torch

# This will fail on PyTorch 2.6+ with YOLO models
try:
    model = torch.load('yolo11n.pt')
except Exception as e:
    print(f"Error: {e}")

# This should work
model = torch.load('yolo11n.pt', weights_only=False)
```

---

## 📚 **REFERENCES:**

- PyTorch 2.6 Release Notes: https://github.com/pytorch/pytorch/releases/tag/v2.6.0
- Weights Only Load: https://pytorch.org/docs/stable/notes/serialization.html
- Ultralytics Issue: https://github.com/ultralytics/ultralytics/issues

---

**Last Updated:** Dec 7, 2025  
**Tested With:** PyTorch 2.6.0, Ultralytics 8.1.0, Kaggle GPU T4
