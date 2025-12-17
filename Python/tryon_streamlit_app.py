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

# Page config - must be first Streamlit command
try:
    st.set_page_config(
        page_title="🐾 PawVerse AI Try-On",
        page_icon="🐾",
        layout="wide",
        initial_sidebar_state="expanded"
    )
except Exception as e:
    # Already configured, skip
    pass

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
    .product-card {
        border: 2px solid #ddd;
        border-radius: 10px;
        padding: 10px;
        margin: 5px;
        text-align: center;
    }
    .product-card:hover {
        border-color: #FF6B35;
        box-shadow: 0 4px 8px rgba(0,0,0,0.1);
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
        st.error("❌ Metadata file not found. Please upload tryon_metadata.json to Kaggle dataset.")
        return None

# Load models (cached)
@st.cache_resource
def load_pipeline():
    """Load AI models - runs once and cached"""
    with st.spinner("🔄 Loading AI models... (This may take 1-2 minutes on first run)"):
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
    "chibi": {
        "name": "Chibi Anime 🎀",
        "description": "Cute anime style with big eyes and vibrant colors"
    },
    "anime": {
        "name": "Anime Style 🌸",
        "description": "Japanese animation style with clean lines"
    },
    "cartoon": {
        "name": "Cartoon 🎪",
        "description": "Playful cartoon illustration style"
    }
}

selected_style = st.sidebar.radio(
    "Select your preferred style:",
    options=list(styles.keys()),
    format_func=lambda x: styles[x]["name"],
    help="Choose the art style for generated image"
)

st.sidebar.info(styles[selected_style]["description"])

# Product selection
st.sidebar.subheader("🛍️ Choose Product")

# Filter products by animal type (default: show all)
animal_filter = st.sidebar.selectbox(
    "Filter by pet type:",
    options=["all", "dog", "cat"],
    format_func=lambda x: "All Pets 🐾" if x == "all" else ("Dogs 🐕" if x == "dog" else "Cats 🐱")
)

# Get compatible products
products = metadata['products']
if animal_filter != "all":
    products = [p for p in products if animal_filter in p['compatible_animals']]

# Product selection with images
product_options = {p['product_id']: p['name_en'] for p in products}
selected_product_id = st.sidebar.selectbox(
    "Select product:",
    options=list(product_options.keys()),
    format_func=lambda x: product_options[x]
)

# Find selected product
selected_product = next(p for p in products if p['product_id'] == selected_product_id)

# Show product details
st.sidebar.divider()
st.sidebar.subheader("📦 Product Details")
st.sidebar.write(f"**Name:** {selected_product['name_en']}")
st.sidebar.write(f"**Category:** {selected_product['category']}")
st.sidebar.write(f"**Compatible:** {', '.join(selected_product['compatible_animals'])}")

# Try to show product image
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
        # Display uploaded image
        input_image = Image.open(uploaded_file)
        st.image(input_image, caption="Your Pet Photo", use_column_width=True)
        
        # Show image info
        st.caption(f"📏 Size: {input_image.size[0]}x{input_image.size[1]}px | Format: {input_image.format}")
    else:
        st.info("👆 Please upload a photo of your pet to get started")
        
        # Show example
        st.markdown("---")
        st.markdown("### 💡 Tips for best results:")
        st.markdown("""
        - ✅ Use a clear, well-lit photo
        - ✅ Pet should be clearly visible
        - ✅ Avoid blurry or dark images
        - ✅ Single pet in the photo
        - ✅ Photo size: 512x512 to 1024x1024 recommended
        """)

with col2:
    st.subheader("✨ Try-On Result")
    
    if uploaded_file is not None:
        # Generate button
        if st.button("🎨 Generate Try-On Image", use_container_width=True):
            # Load pipeline
            if st.session_state.pipeline is None:
                st.session_state.pipeline = load_pipeline()
            
            if st.session_state.pipeline is not None:
                # Create progress container
                progress_container = st.container()
                
                with progress_container:
                    # Progress bar
                    progress_bar = st.progress(0)
                    status_text = st.empty()
                    
                    try:
                        # Step 1: Detect animal
                        status_text.text("🔍 Detecting pet...")
                        progress_bar.progress(20)
                        time.sleep(0.5)
                        
                        detection_result = st.session_state.pipeline.detect_animal(input_image)
                        
                        if not detection_result['detected']:
                            st.error("❌ No pet detected in the image. Please upload a clear photo of a dog or cat.")
                            st.stop()
                        
                        # Show detection info
                        st.success(f"✅ Detected: {detection_result['animal_type'].upper()} (confidence: {detection_result['confidence']:.2%})")
                        
                        # Step 2: Generate
                        status_text.text("🎨 Generating try-on image...")
                        progress_bar.progress(40)
                        
                        result = st.session_state.pipeline.generate(
                            image=input_image,
                            product_id=selected_product_id,
                            style_id=selected_style,
                            animal_type=detection_result['animal_type'],
                            progress_callback=lambda p: progress_bar.progress(40 + int(p * 0.55))
                        )
                        
                        # Complete
                        progress_bar.progress(100)
                        status_text.text("✅ Generation complete!")
                        time.sleep(0.5)
                        
                        # Clear progress
                        progress_container.empty()
                        
                        # Store result
                        st.session_state.result_image = result
                        
                        # Display result
                        st.image(result['image'], caption="Try-On Result", use_column_width=True)
                        
                        # Show metadata
                        st.success(f"⏱️ Generated in {result['processing_time']:.1f}s")
                        
                        # Download button
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
                        st.exception(e)
        
        # Show previous result if exists
        elif st.session_state.result_image is not None:
            st.image(st.session_state.result_image['image'], caption="Previous Result", use_column_width=True)
            st.info("👆 Upload a new image or change settings, then click Generate again")
    else:
        st.info("👈 Upload a pet photo to generate try-on image")
        
        # Show sample results
        st.markdown("---")
        st.markdown("### 🎨 Sample Results")
        st.markdown("*Example outputs will appear here after generation*")

# Footer
st.divider()
st.markdown("""
<div style='text-align: center; color: #666; padding: 1rem;'>
    <p>🐾 <strong>PawVerse AI Try-On</strong> | Powered by YOLO11 + Stable Diffusion + ControlNet</p>
    <p>Made with ❤️ for pet lovers | Demo for educational purposes</p>
</div>
""", unsafe_allow_html=True)

# Sidebar footer
st.sidebar.divider()
st.sidebar.markdown("""
### 📊 System Info
- **GPU:** Check runtime settings
- **Models:** YOLO11 + SD1.5 + ControlNet
- **Style Presets:** 3 available
- **Products:** 6 available

### ⚡ Performance
- Detection: ~0.5s
- Generation: ~10-15s
- Total: ~12-17s
""")

st.sidebar.success("✅ App ready!")
