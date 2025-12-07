# 1. Tạo virtual environment
cd d:\1Hutech\workspace05102025\PawVerseAPI (Đường dẫn tới workspace của dự án)
python -m venv venv

# 2. Kích hoạt (Windows)
venv\Scripts\activate

# Bạn sẽ thấy (venv) ở đầu dòng:
# (venv) D:\1Hutech\workspace05102025\PawVerseAPI> 

# 3. Upgrade pip
python -m pip install --upgrade pip

# 4. Cài đặt dependencies
pip install -r Python/requirements.txt

# 5. Test CUDA
python -c "import torch; print('CUDA:', torch.cuda.is_available())"

# 6. Test script
python Python/breed_detection.py --init-only

# If error CUDA not available, run:
pip install torch torchvision --index-url https://download.pytorch.org/whl/cu118

# 7. Run .NET API (in venv or outside both OK)
dotnet run

# Expected logs:
# [ModelManager] Loading models...
# [ModelManager] All models loaded in 3.34s
# Breed detection models initialized successfully

# 8. Test API
# GET https://localhost:7038/api/breed-detection/status
# POST https://localhost:7038/api/breed-detection/detect (with image upload)