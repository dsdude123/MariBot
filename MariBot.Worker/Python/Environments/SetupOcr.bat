conda create --name=ocr python=3.9
conda activate ocr
conda install pytorch torchvision torchaudio pytorch-cuda=11.7 -c pytorch -c nvidia
pip install easyocr