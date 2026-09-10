import re
import sys

import torch
from diffusers import StableDiffusion3Pipeline
from huggingface_hub import login

from _paths import model_cache, read_prompt

model_id = "stabilityai/stable-diffusion-3-medium-diffusers"

prompt_path = sys.argv[1]
output_path = sys.argv[2]
hf_token = sys.argv[3] if len(sys.argv) > 3 else None

# A gated model: without a token that has accepted its licence the download 401s.
if hf_token:
    login(token=hf_token)

prompt = read_prompt(prompt_path)

negative_arr = re.findall(r"\(+.*?\)+", prompt)
prompt = re.sub(r"\(+.*?\)+", "", prompt)
prompt_negative = " ".join(negative_arr).replace("(", "").replace(")", "")

print(f"Prompt is: {prompt}")
print(f"Negative Prompt is: {prompt_negative}")

pipe = StableDiffusion3Pipeline.from_pretrained(
    model_id,
    torch_dtype=torch.float16,
    cache_dir=model_cache(),
)
pipe.to("cuda")

image = pipe(
    prompt,
    negative_prompt=prompt_negative,
    num_inference_steps=28,
    guidance_scale=7.0,
).images[0]
image.save(output_path)
