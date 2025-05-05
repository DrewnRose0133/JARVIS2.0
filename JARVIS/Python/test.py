from resemblyzer import VoiceEncoder, preprocess_wav
import numpy as np

encoder = VoiceEncoder()

w1 = preprocess_wav("Profiles/drew.wav")
w2 = preprocess_wav("Profiles/wake_word.wav")

e1 = encoder.embed_utterance(w1)
e2 = encoder.embed_utterance(w2)

score = np.inner(e1, e2)
print(f"Similarity: {score:.4f}")
