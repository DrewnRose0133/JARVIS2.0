from resemblyzer import VoiceEncoder, preprocess_wav
import numpy as np
import sys
import os
import logging

# Suppress internal logs
logging.getLogger("resemblyzer").setLevel(logging.ERROR)

# Ensure consistent base path
base_dir = os.path.dirname(os.path.abspath(__file__))
profiles_path = os.path.join(base_dir, "profiles.npy")

# Load voice profiles
profiles = np.load(profiles_path, allow_pickle=True).item()
encoder = VoiceEncoder()

def identify_user(wav_path):
    wav = preprocess_wav(wav_path)
    embed = encoder.embed_utterance(wav)

    best_match = None
    best_score = -1

    for name, ref in profiles.items():
        score = np.inner(embed, ref)
        if score > best_score:
            best_score = score
            best_match = name

    return best_match if best_score > 0.60 else "unknown"

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("unknown")
    else:
        result = identify_user(sys.argv[1])
        print(result.strip().lower())  # ✅ Output ONLY the username in lowercase
