# Sparse Converter

*SparseConverter* is a tool that can create / decompress compressed EXT4 filesystem sparse image format (e.g. super.img.0).
It can also decompress sparse Android data image (e.g. system.new.dat) into an EXT4 filesystem image.

**DISCLAMER**: The software may contain bugs and/or limitations. The authors take no responsibility for any damage/s that may occur.

## Usage Examples

`SparseConverter.exe /compress D:\system.img E:\ 256MB`  
will compress D:\system.img to 256MB sparse files starting from E:\system.img_sparsechunk1  
`SparseConverter.exe /decompress E:\super.img.0 D:\system.img`  
will decompress E:\super.img.0, E:\super.img.1, etc. to D:\system.img  
`SparseConverter.exe /decompressData E:\system.transfer.list E:\system.new.dat D:\system.img`  
will decompress E:\system.new.dat to D:\system.img using instructions in E:\system.transfer.list

## CREDITS

[@TalAloni](https://github.com/TalAloni) for the upstream repo  
[@xpirt](https://github.com/xpirt) for the [Python code](https://github.com/xpirt/sdat2img/blob/1b08432247fce8037fd6a43685c6e7037a2e553a/sdat2img.py) on which /decompressData is based on