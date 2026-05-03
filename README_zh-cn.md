<div align="center">
  <img width="100" height="100" alt="RX-SSDV" src="https://github.com/user-attachments/assets/b9c2d32c-a9e8-47d0-afd3-b29a25dc17b2"/>
  <h1>RX-SSDV</h1>
  <a href="https://github.com/AstarLC4036/RX-SSDV/blob/main/README.md">English README</a> | <a href="https://github.com/AstarLC4036/RX-SSDV/blob/main/README_zh-cn.md">中文 README</a>
</div>
<br>

# RX-SSDV (zh-cn)
一个基于 `WPF` 的简单SSDV基带解码器。<br>
> 使用的解码器: [SSDV](https://github.com/fsphil/ssdv)<br>
**注意**: 这个项目还在开发，所以可能有大量的Bug并且许多功能不太完善。

### 提示
用虚拟声卡作为音频输入(在Windows设置中更改)以获得更佳的解调效果。

## 如何使用
*记得在解码前选择合适的解码器类型哦*
### 使用基带作为采样源
*注意: 有限的功能*
1. 将 `Sample Source` 设置为 `Baseband File`
2. 点击 `Browse` 按钮来选择目标文件
3. 勾选 `Enable Process`
4. 点击 `Play` 按钮并且等待解码器处理完成

### 使用声卡作为采样源
1. 将 `Sample Source` 设置为 `Sound Card`.
2. 点击 `Play` 按钮来开始录制
3. 播放音频 *(Raw)* 并且等待解码器处理完成

## 支持的模式
### 支持的卫星
- ASRTU-1(AO-123) - BPSK 9600bps & CCSDS Concatenated (SSDV)

### 支持的调制
- BPSK

### 支持的协议
- CCSDS Concatenated
