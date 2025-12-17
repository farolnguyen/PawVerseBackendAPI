# 🚀 Kaggle Setup Guide - PawVerse AI Try-On

Complete guide để setup và chạy demo trên Kaggle.

---

## 📋 **TABLE OF CONTENTS**

1. [Prerequisites](#prerequisites)
2. [Prepare Data](#prepare-data)
3. [Create Kaggle Datasets](#create-kaggle-datasets)
4. [Upload Notebook](#upload-notebook)
5. [Configure & Run](#configure-run)
6. [Troubleshooting](#troubleshooting)

---

## 🎯 **PREREQUISITES**

### **1. Kaggle Account**
- ✅ Create free account at [kaggle.com](https://www.kaggle.com/)
- ✅ Verify email
- ✅ Phone verification (required for GPU access)

### **2. Ngrok Account** (for public URL)
- ✅ Create free account at [ngrok.com](https://ngrok.com/)
- ✅ Get auth token from [dashboard](https://dashboard.ngrok.com/get-started/your-authtoken)

### **3. Local Files Prepared**
```
📁 Local folder to prepare:
├── tryon_metadata.json           ← From wwwroot/
├── datatryon/                    ← Product images
│   ├── batancham.png
│   ├── batandoi.png
│   ├── chuong_huan_luyen.png
│   ├── nemdabao.png
│   ├── ro_mom_hong.png
│   └── vongco.png
├── inference_pipeline.py         ← Created
├── tryon_streamlit_app.py        ← Created
└── kaggle_tryon_notebook.ipynb   ← Created
```

---

## 📦 **PREPARE DATA**

### **Step 1: Copy Metadata**

```bash
# Copy metadata JSON to a new folder
mkdir kaggle_upload
cp D:\1Hutech\workspace05102025\PawVerseAPI\wwwroot\tryon_metadata.json kaggle_upload/
```

### **Step 2: Copy Product Images**

```bash
# Copy product images
mkdir kaggle_upload\products
mkdir kaggle_upload\products\datatryon

# Copy all PNG files
cp D:\1Hutech\workspace05102025\PawVerseAPI\wwwroot\Images\datatryon\*.png kaggle_upload\products\datatryon\
```

Verify you have:
```
kaggle_upload/
├── tryon_metadata.json           ✅ ~50KB
└── products/
    └── datatryon/
        ├── batancham.png         ✅ ~89KB
        ├── batandoi.png          ✅ ~90KB
        ├── chuong_huan_luyen.png ✅ ~212KB
        ├── nemdabao.png          ✅ ~100KB
        ├── ro_mom_hong.png       ✅ ~154KB
        └── vongco.png            ✅ ~69KB

Total size: ~764KB (very small!)
```

---

## 🗄️ **CREATE KAGGLE DATASETS**

### **Dataset 1: Metadata**

1. **Go to Kaggle:**
   - Navigate to [kaggle.com/datasets](https://www.kaggle.com/datasets)
   - Click "New Dataset"

2. **Upload:**
   - Click "Select Files to Upload"
   - Select `tryon_metadata.json`
   - Wait for upload to complete

3. **Configure:**
   ```
   Title: PawVerse Try-On Metadata
   Slug: tryon-metadata (important!)
   Description: Product metadata for AI try-on demo
   License: Database Contents License (DbCL)
   Visibility: Public
   ```

4. **Click "Create"**

5. **Note the path:**
   ```
   /kaggle/input/tryon-metadata/tryon_metadata.json
   ```

---

### **Dataset 2: Product Images**

1. **Create New Dataset:**
   - Click "New Dataset" again

2. **Upload:**
   - Click "Select Files to Upload"
   - **Upload entire folder:** Select `products` folder
   - Kaggle will maintain folder structure
   - Wait for all 6 images to upload

3. **Configure:**
   ```
   Title: PawVerse Try-On Products
   Slug: tryon-products (important!)
   Description: Product images for AI try-on demo (6 PNG files)
   License: CC0: Public Domain
   Visibility: Public
   ```

4. **Click "Create"**

5. **Note the path:**
   ```
   /kaggle/input/tryon-products/datatryon/*.png
   ```

---

### **Verify Datasets**

Go to your profile → Datasets → You should see:
- ✅ **tryon-metadata** (1 file, ~50KB)
- ✅ **tryon-products** (6 files, ~764KB)

---

## 📓 **UPLOAD NOTEBOOK**

### **Option A: Upload via Web (Easiest)**

1. **Go to Kaggle Notebooks:**
   - Navigate to [kaggle.com/code](https://www.kaggle.com/code)
   - Click "New Notebook"

2. **Upload Notebook File:**
   - Click File → Upload Notebook
   - Select `kaggle_tryon_notebook.ipynb`
   - Wait for upload

3. **Configure Notebook:**
   - Title: "PawVerse AI Try-On Demo"
   - Click "Save Version"

---

### **Option B: Create from Scratch**

1. **Create New Notebook**

2. **Copy Content:**
   - Open `kaggle_tryon_notebook.ipynb` locally
   - Copy all cells one by one to Kaggle

3. **Add Python Files:**
   - In notebook, use `%%writefile` cells
   - Copy content of `inference_pipeline.py`
   - Copy content of `tryon_streamlit_app.py`

---

## ⚙️ **CONFIGURE & RUN**

### **Step 1: Add Datasets to Notebook**

1. **In Kaggle Notebook:**
   - Click "Add Data" (right sidebar)
   - Search "tryon-metadata"
   - Click "+" to add
   - Search "tryon-products"
   - Click "+" to add

2. **Verify paths:**
   - Check "Data" panel shows both datasets
   - Paths should be:
     - `/kaggle/input/tryon-metadata/`
     - `/kaggle/input/tryon-products/`

---

### **Step 2: Enable GPU**

1. **Settings (right sidebar):**
   - Accelerator: **GPU T4 x2** ⚡
   - Environment: **Python** (default)
   - Internet: **ON** 🌐

2. **Verify GPU:**
   ```python
   import torch
   print(torch.cuda.is_available())  # Should be True
   ```

---

### **Step 3: Configure ngrok**

1. **Get your token:**
   - Go to [ngrok dashboard](https://dashboard.ngrok.com/get-started/your-authtoken)
   - Copy your auth token

2. **In notebook (Step 5 cell):**
   ```python
   NGROK_TOKEN = "YOUR_TOKEN_HERE"  # Replace with your token
   ```

---

### **Step 4: Run Notebook**

1. **Run all cells in order:**
   - Click "Run All" or
   - Run cells one by one with Shift+Enter

2. **Wait for setup** (~2-3 minutes):
   ```
   ✅ Installing packages... (30s)
   ✅ Loading models... (60-90s)
   ✅ Starting Streamlit... (10s)
   ✅ Creating public URL... (5s)
   ```

3. **Get the URL:**
   ```
   🌐 Public URL: https://xxxx-xx-xxx-xxx-xx.ngrok.io
   ```

4. **Copy & Share:**
   - Copy the ngrok URL
   - Open in browser
   - Share with giảng viên

---

## 🎨 **USING THE APP**

1. **Open URL in browser**

2. **Interface:**
   ```
   🐾 PawVerse AI Try-On
   
   [Sidebar]
   - Choose Style (3 options)
   - Choose Product (6 options)
   
   [Main Area]
   - Upload Pet Photo
   - Generate Button
   - View Result
   ```

3. **Workflow:**
   - Select style (e.g., "Chibi Anime")
   - Select product (e.g., "Slow Feeder Bowl")
   - Upload pet photo (JPG/PNG)
   - Click "Generate Try-On Image"
   - Wait 10-15s
   - View & download result!

---

## 🐛 **TROUBLESHOOTING**

### **Issue 1: "Dataset not found"**

**Error:**
```
FileNotFoundError: /kaggle/input/tryon-metadata/tryon_metadata.json
```

**Fix:**
1. Check dataset is added to notebook
2. Check dataset slug matches exactly: `tryon-metadata`
3. Re-add dataset via "Add Data" button

---

### **Issue 2: "No GPU available"**

**Error:**
```
CUDA available: False
```

**Fix:**
1. Settings → Accelerator → Select "GPU T4 x2"
2. Save & Run again
3. If still fails, check Kaggle quota (30h/week free GPU)

---

### **Issue 3: "ngrok error"**

**Error:**
```
ERROR: Invalid auth token
```

**Fix:**
1. Double-check token from [ngrok dashboard](https://dashboard.ngrok.com/get-started/your-authtoken)
2. Make sure you replaced "YOUR_TOKEN_HERE" with actual token
3. Check no extra spaces in token

---

### **Issue 4: "Out of Memory"**

**Error:**
```
CUDA out of memory
```

**Fix:**
1. Restart notebook (Session → Restart)
2. Enable optimizations:
   ```python
   pipe.enable_attention_slicing(1)
   pipe.enable_xformers_memory_efficient_attention()
   ```
3. Reduce steps:
   ```python
   num_inference_steps=15  # Instead of 20
   ```

---

### **Issue 5: "Models loading slow"**

**Issue:** Models take >5 minutes to load

**Fix:**
- Normal! First time download from HuggingFace
- Models are cached after first run
- Subsequent runs will be faster (~30s)

---

### **Issue 6: "Streamlit not accessible"**

**Error:** URL opens but shows "This site can't be reached"

**Fix:**
1. Check firewall/antivirus
2. Try different browser
3. Check ngrok status:
   ```python
   import pyngrok
   pyngrok.ngrok.get_tunnels()
   ```
4. Restart streamlit:
   ```bash
   !pkill -f streamlit
   !streamlit run tryon_app.py --server.port 8501 &
   ```

---

## 📊 **PERFORMANCE EXPECTATIONS**

### **With GPU T4 (16GB VRAM):**

| Stage | Time | Notes |
|-------|------|-------|
| Model Loading | 60-90s | First time only |
| Pet Detection | 0.3-0.5s | YOLO11 |
| Canny Generation | 0.1s | OpenCV |
| SD Generation | 8-12s | 20 steps |
| **Total per image** | **10-15s** | ✅ Good |

### **Memory Usage:**
```
YOLO11: ~500MB
SD 1.5: ~3.8GB
ControlNet: ~1.5GB
PyTorch: ~500MB
Total: ~6.3GB / 16GB (39% usage) ✅
```

---

## 🎯 **DEMO CHECKLIST**

Before presenting to giảng viên:

- [ ] ✅ Both datasets uploaded & public
- [ ] ✅ Notebook runs without errors
- [ ] ✅ GPU enabled (T4)
- [ ] ✅ ngrok token configured
- [ ] ✅ Public URL working
- [ ] ✅ Test with at least 2 products
- [ ] ✅ Test with all 3 styles
- [ ] ✅ Take screenshots of results
- [ ] ✅ Record demo video (optional)

---

## 🎥 **RECOMMENDED DEMO FLOW**

1. **Show Problem Statement** (1 min)
   - Hardware constraints (GTX 1650 4GB)
   - Why Kaggle solution

2. **Show Notebook Setup** (1 min)
   - Datasets
   - GPU configuration
   - Quick overview of code

3. **Live Demo** (5 min)
   - Open public URL
   - Select product & style
   - Upload pet photo
   - Generate result
   - Show 2-3 different combinations

4. **Show Results** (2 min)
   - Different styles comparison
   - Different products comparison
   - Discuss quality

5. **Q&A** (2 min)

---

## 📚 **ADDITIONAL RESOURCES**

### **Kaggle Limits (Free Tier):**
- GPU: 30 hours/week
- Disk: 20GB
- RAM: 32GB
- Internet: Enabled

### **Useful Links:**
- Kaggle Docs: https://www.kaggle.com/docs
- ngrok Docs: https://ngrok.com/docs
- Streamlit Docs: https://docs.streamlit.io

### **Alternative Tunneling (if ngrok doesn't work):**
```bash
# Option 1: Cloudflare Tunnel
!cloudflared tunnel --url http://localhost:8501

# Option 2: LocalTunnel  
!npx localtunnel --port 8501
```

---

## 🆘 **NEED HELP?**

If you encounter issues:

1. **Check Kaggle logs:**
   - Scroll down in notebook output
   - Look for error messages in red

2. **Test components individually:**
   - Run "Test Pipeline" cell
   - Check if models load successfully

3. **Restart clean:**
   - Session → Restart & Clear Output
   - Run All again

4. **Check dataset paths:**
   ```python
   !ls /kaggle/input/
   !ls /kaggle/input/tryon-metadata/
   !ls /kaggle/input/tryon-products/datatryon/
   ```

---

## ✅ **SUCCESS INDICATORS**

You're ready to demo when you see:

```
🎉 STREAMLIT APP IS READY!
=====================================
🌐 Public URL: https://xxxx.ngrok.io
📱 Local URL: http://localhost:8501
💡 Share this URL with your teacher!
⚠️ Keep this notebook running
=====================================
```

**Good luck with your demo! 🚀🐾**

---

**Last Updated:** Dec 7, 2025  
**Version:** 1.0  
**Author:** PawVerse Team
