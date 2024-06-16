import torch
import sys
import json
from detoxify import Detoxify

torch.hub.set_dir("./hub-cache")
model = torch.hub.load('unitaryai/detoxify','multilingual_toxic_xlm_r')

# Read parameters from command line
request_guid = sys.argv[1]

# Load text prompt into file
file = open(f".{chr(92)}Python{chr(92)}{request_guid}.txt")
prompt = file.read()
prompt = prompt.replace('"', '')
file.close()

results = Detoxify('multilingual', device='cuda').predict([prompt])
print(results)

json_object = json.dumps(results)
with open(f".{chr(92)}Python{chr(92)}{request_guid}-moderation.json", "w") as outfile:
    outfile.write(json_object)

