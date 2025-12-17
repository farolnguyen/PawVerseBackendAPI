@echo off
REM Prepare Kaggle Upload - PawVerse Try-On
REM This script prepares all files needed for Kaggle

echo ==========================================
echo  PawVerse - Kaggle Upload Preparation
echo ==========================================
echo.

REM Set paths
set BASE_DIR=D:\1Hutech\workspace05102025\PawVerseAPI
set OUTPUT_DIR=%BASE_DIR%\Python\kaggle_upload

echo [1/5] Creating folder structure...
if exist "%OUTPUT_DIR%" (
    echo   Cleaning existing folder...
    rmdir /s /q "%OUTPUT_DIR%"
)

mkdir "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%\metadata"
mkdir "%OUTPUT_DIR%\products"
mkdir "%OUTPUT_DIR%\products\datatryon"
echo   Done!

echo.
echo [2/5] Copying metadata...
copy "%BASE_DIR%\wwwroot\tryon_metadata.json" "%OUTPUT_DIR%\metadata\" >nul
echo   Copied: tryon_metadata.json

echo.
echo [3/5] Copying product images...
copy "%BASE_DIR%\wwwroot\Images\datatryon\*.png" "%OUTPUT_DIR%\products\datatryon\" >nul
echo   Copied 6 product images

echo.
echo [4/5] Copying Python files...
copy "%BASE_DIR%\Python\inference_pipeline.py" "%OUTPUT_DIR%\" >nul
copy "%BASE_DIR%\Python\tryon_streamlit_app.py" "%OUTPUT_DIR%\" >nul
copy "%BASE_DIR%\Python\requirements_kaggle.txt" "%OUTPUT_DIR%\" >nul
echo   Copied Python scripts

echo.
echo [5/5] Creating README...
echo PawVerse AI Try-On - Kaggle Upload Package > "%OUTPUT_DIR%\README.txt"
echo. >> "%OUTPUT_DIR%\README.txt"
echo Upload Instructions: >> "%OUTPUT_DIR%\README.txt"
echo 1. Upload 'metadata' folder as Kaggle Dataset (slug: tryon-metadata) >> "%OUTPUT_DIR%\README.txt"
echo 2. Upload 'products' folder as Kaggle Dataset (slug: tryon-products) >> "%OUTPUT_DIR%\README.txt"
echo 3. See KAGGLE_SETUP_GUIDE.md for complete instructions >> "%OUTPUT_DIR%\README.txt"
echo   Done!

echo.
echo ==========================================
echo  SUCCESS! Files prepared at:
echo  %OUTPUT_DIR%
echo ==========================================
echo.
echo Next steps:
echo 1. Open folder: %OUTPUT_DIR%
echo 2. Upload to Kaggle as instructed in KAGGLE_SETUP_GUIDE.md
echo 3. Create notebook and run!
echo.

REM Open folder
start "" "%OUTPUT_DIR%"

echo Press any key to exit...
pause >nul
