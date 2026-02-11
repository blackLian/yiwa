# 3DGS 录入与建模 Pipeline（MVP）

## 输入

- 手机环拍视频：30~60 秒
- 目标：覆盖猫咪全身，尽量匀速绕拍一圈

## 步骤

1. 抽帧（FFmpeg）
2. 相机位姿估计（COLMAP/SfM）
3. 3D Gaussian Splatting 重建
4. 质量检测（模糊、曝光、遮挡）
5. 导出与资产登记（PLY/OBJ/FBX/GLTF 或原生 3DGS）

## 示例命令

```bash
ffmpeg -i input.mp4 -vf "fps=8" frames/frame_%04d.png
```

## QC 建议阈值

- 模糊帧比例 < 15%
- 有效覆盖角度 >= 270°
- 过曝/欠曝帧比例 < 20%

## 输出

- `assets/pets/<pet_id>_*.3dgs`
- `assets/pets/<pet_id>_profile.json`
