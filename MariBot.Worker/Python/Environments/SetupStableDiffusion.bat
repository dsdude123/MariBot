conda create --name ldm python=3.9
conda activate ldm
pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu117
pip install diffusers transformers accelerate scipy safetensors protobuf sentencepiece
pip install xformers