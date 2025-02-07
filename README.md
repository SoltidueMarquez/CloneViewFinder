# 效果展示视频

https://www.bilibili.com/video/BV1VVNteiEgg/?vd_source=cee09b05f3a3e6487b6158deaaf10d4d

# 笔记
  1.切割物体（重点是获取切割平面与模型三角形截点，并根据这些截点构造新的三角形面，以及填充切割平面）

  2.创建四个平面（视锥平面）以及视锥物体；利用平面进行切割，拍照时保存视野内物体；放置照片时，再进行一次切割但此时剔除而不是保存视野内物体，随后放置前面保存好的物体

总结：思路其实很简单，难点在于实现比较繁琐细微。TNTC作者视频中提供了一个实现，但是在体验中有时会有物体突然消失等问题，以及切割过程中没有共享顶点（shared vertex）。针对以上两部分重新实现了ViewFinder核心机制（Slicer 用于实现切割，Polaroid 用于实现ViewFinder核心机制），但模型及动画使用了TNTC作者仓库中资产。

# 参考链接
## 切割物体

Kristin Lague： https://www.youtube.com/watch?v=1UsuZsaUUng&t=62s

Tvtig： https://www.youtube.com/watch?v=BVCNDUcnE1o&t=1s

## ViewFinder照片机制

TNTC： https://www.youtube.com/watch?v=rdExK08zYBI&t=264s
