<div align="center">
  <img width="100" height="100" alt="RX-SSDV" src="https://github.com/user-attachments/assets/b9c2d32c-a9e8-47d0-afd3-b29a25dc17b2"/>
  <h1>RX-SSDV</h1>
  <a href="https://github.com/AstarLC4036/RX-SSDV/blob/main/README.md">English README</a> | <a href="https://github.com/AstarLC4036/RX-SSDV/blob/main/README_zh-cn.md">中文 README</a>
</div>
<br>

# RX-SSDV (en-us)
A simple baseband SSDV decoder based on `WPF`.<br>
> Decoder used: [SSDV](https://github.com/fsphil/ssdv)<br>
**Note**: This project is still under development, so there may be a large number of bugs and the features are not very complete.

### Tips
Use a virtual sound card as audio input(in windows settings) to achieve better demodulation results.

## How to use
*Please select the required decoder type before starting processing*
### Use baseband as sample source
*Note: Limited function*
1. Set `Sample Source` to `Baseband File`
2. Click `Browse` button to select target file.
3. Toggle `Enable Process` checkbox.
4. Click `Play` button and wait for the decoding process to complete.

### Use sound card as sample source  
1. Set `Sample Source` to `Sound Card`.
2. Click `Play` button to start recoding.
3. Play the audio *(Raw)* and wait for the decoding process to complete.

## Supported Modes
### Supported Satellites
- ASRTU-1(AO-123) - BPSK 9600bps & CCSDS Concatenated (SSDV)

### Supported Modulations
- BPSK

### Supported Protocol
- CCSDS Concatenated
