import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets" / "pets"


def load_profile(name: str):
    p = ASSETS / name
    data = json.loads(p.read_text(encoding="utf-8"))
    assert data["id"], "id required"
    assert data["displayName"], "displayName required"
    poses = data["poseAssets"]
    assert len(poses) >= 3, f"{name}: requires >=3 poses"
    assert all(poses), f"{name}: empty pose"
    return data


def infer_mouse_bias(profile: dict) -> float:
    b = profile["behavior"]
    if b.get("mouseApproachProbability", 0) > 0:
        return b["mouseApproachProbability"]
    return -b.get("mouseAvoidProbability", 0)


def main() -> None:
    cute = load_profile("cute_cat_profile.json")
    cool = load_profile("cool_cat_profile.json")

    assert infer_mouse_bias(cute) > 0, "cute should approach mouse"
    assert infer_mouse_bias(cool) < 0, "cool should avoid mouse"

    print("ok: profiles valid and personality direction is correct")


if __name__ == "__main__":
    main()
