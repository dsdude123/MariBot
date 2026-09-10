import json
import sys

import torch
from detoxify import Detoxify

from _paths import model_cache, read_prompt

# Arguments, in place of the guid this used to rebuild Windows paths from.
prompt_path = sys.argv[1]
output_path = sys.argv[2]

torch.hub.set_dir(model_cache())

device = "cuda" if torch.cuda.is_available() else "cpu"

results = Detoxify("multilingual", device=device).predict([read_prompt(prompt_path)])

# The worker deserialises this into ToxcityResult, whose fields are arrays.
with open(output_path, "w", encoding="utf-8") as outfile:
    outfile.write(json.dumps(results))
