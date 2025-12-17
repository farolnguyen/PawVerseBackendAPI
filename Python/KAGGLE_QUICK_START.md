# ⚡ Kaggle Quick Start - 5 Minutes Setup

Hướng dẫn nhanh để chạy demo trong 5 phút!

---

## 🚀 **SUPER QUICK SETUP**

### **Step 1: Prepare Files (1 minute)**

```bash
# Run this script (Windows):
cd D:\1Hutech\workspace05102025\PawVerseAPI\Python
prepare_kaggle_upload.bat
```

**Output:** Folder `kaggle_upload` with:
```
kaggle_upload/
├── metadata/
│   └── tryon_metadata.json      ← Dataset 1
├── products/
│   └── datatryon/               ← Dataset 2
│       ├── batancham.png
│       ├── batandoi.png
│       └── ... (6 images)
├── inference_pipeline.py
├── tryon_streamlit_app.py
└── requirements_kaggle.txt
```

---

### **Step 2: Upload to Kaggle (2 minutes)**

#### **Dataset 1: Metadata**
1. Go to [kaggle.com/datasets](https://www.kaggle.com/datasets) → "New Dataset"
2. Upload: `kaggle_upload/metadata/tryon_metadata.json`
3. Title: "PawVerse Try-On Metadata"
4. **Slug: `tryon-metadata`** ⚠️ Important!
5. Create

#### **Dataset 2: Products**
1. "New Dataset" again
2. Upload entire folder: `kaggle_upload/products/datatryon/`
3. Title: "PawVerse Try-On Products"  
4. **Slug: `tryon-products`** ⚠️ Important!
5. Create

---

### **Step 3: Create Notebook (1 minute)**

1. Go to [kaggle.com/code](https://www.kaggle.com/code) → "New Notebook"
2. File → Upload Notebook → Select `kaggle_tryon_notebook.ipynb`
3. Add Data → Search "tryon-metadata" → Add
4. Add Data → Search "tryon-products" → Add
5. Settings:
   - Accelerator: **GPU T4 x2** ⚡
   - Internet: **ON** 🌐

---

### **Step 4: Get ngrok Token (30 seconds)**

1. Go to [ngrok.com](https://ngrok.com/) → Sign up free
2. Dashboard → [Your Authtoken](https://dashboard.ngrok.com/get-started/your-authtoken)
3. Copy token (looks like: `2abc...xyz`)

---

### **Step 5: Run! (30 seconds)**

1. In notebook, find Step 5 cell:
   ```python
   NGROK_TOKEN = "YOUR_NGROK_TOKEN_HERE"
   ```
2. Replace with your token
3. Click "Run All"
4. Wait 2-3 minutes for models to load
5. Copy the public URL that appears:
   ```
   🌐 Public URL: https://xxxx.ngrok.io
   ```
6. Open URL in browser
7. **Done!** 🎉

---

## 🎨 **USING THE APP**

```
1. Select Style:     [Chibi / Anime / Cartoon]
2. Select Product:   [Bowl / Collar / Bell / ...]
3. Upload pet photo: [Browse...]
4. Click "Generate"
5. Wait 10-15s
6. View result!
7. Download image
```

---

## 📸 **DEMO TIPS**

### **Best Results:**
- ✅ Clear, well-lit pet photos
- ✅ Single pet in frame
- ✅ Pet facing camera
- ✅ Image size: 512x512 to 1024x1024

### **What to Show:**
1. **Different products:**
   - Bowl on ground
   - Collar on neck
   - Bed under pet

2. **Different styles:**
   - Chibi (cute anime)
   - Anime (clean)
   - Cartoon (playful)

3. **Different pets:**
   - Dog photos
   - Cat photos

---

## ⚠️ **IMPORTANT NOTES**

### **Kaggle Limits:**
- 30 hours GPU/week (free)
- Keep notebook running while demoing
- Session timeout: 12 hours max

### **ngrok Limits:**
- Free: 1 tunnel, 40 connections/min
- Tunnel expires if notebook restarts
- Get new URL if reconnecting

### **Common Issues:**

| Issue | Fix |
|-------|-----|
| "Dataset not found" | Check dataset slug matches exactly |
| "No GPU" | Settings → GPU T4 x2 |
| "ngrok error" | Double-check token |
| "OOM" | Reduce inference steps to 15 |

---

## 🎯 **PRESENTATION CHECKLIST**

Before demo:
- [ ] Both datasets uploaded & public
- [ ] Notebook runs successfully  
- [ ] Public URL accessible
- [ ] Test 2+ products
- [ ] Test all 3 styles
- [ ] Screenshot results
- [ ] Prepare 2-3 sample pet photos

---

## 📱 **SHARE WITH TEACHER**

```
Hello Professor,

Here's the demo for PawVerse AI Try-On:

🌐 URL: https://xxxx.ngrok.io

Features:
- 6 pet products
- 3 art styles
- ~10-15s generation time

Please upload a clear dog/cat photo and try it!

Note: Keep this link open during demo.
The system runs on Kaggle GPU T4.

Thank you!
```

---

## 🆘 **EMERGENCY HELP**

If something breaks during demo:

1. **Notebook crashed?**
   - Session → Restart & Run All
   - Get new ngrok URL

2. **URL not working?**
   - Check firewall
   - Try different browser
   - Refresh ngrok tunnel

3. **Generation failed?**
   - Try different image
   - Reduce to 512x512
   - Restart pipeline

4. **Out of time?**
   - Use pre-generated screenshots
   - Explain the concept with diagrams

---

## ✅ **SUCCESS = SEEING THIS**

```
🎉 STREAMLIT APP IS READY!
🌐 Public URL: https://xxxx.ngrok.io
```

Then open URL → Upload photo → Generate → **WOW! 🎨**

---

**Good luck! You got this! 🚀🐾**

---

**Questions?** Check `KAGGLE_SETUP_GUIDE.md` for detailed help.
