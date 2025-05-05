# generate_profiles.py

from resemblyzer import VoiceEncoder, preprocess_wav
from pathlib import Path
import numpy as np

encoder = VoiceEncoder()
profiles = {}

print("Generating voice profiles from Profiles/*.wav...\n")

for file in Path("Profiles").glob("*.wav"):
    print(f"Processing {file.name}")
    wav = preprocess_wav(file)
    embed = encoder.embed_utterance(wav)
    profiles[file.stem] = embed

np.save("profiles.npy", profiles)
print("\nVoice profiles saved to profiles.npy")
