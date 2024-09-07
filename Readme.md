# Sparse Converter

*SparseConverter* is a tool that can create / decompress compressed EXT4 file system sparse image (e.g. super.img.0).
It can also decompress sparse Android data image (e.g. system.new.dat) into an EXT4 file system image.

**DISCLAMER**: The software may contain bugs and/or limitations. The authors take no responsibility for any damage/s that may occur.

## Usage Examples

`SparseConverter.exe /c[ompress] D:\system.img E:\ 256MB`  
will compress D:\system.img to 256MB sparse files starting from E:\system.img_sparsechunk1  
`SparseConverter.exe /d[ecompress] E:\super.img.0 D:\system.img`  
will decompress E:\super.img.0, E:\super.img.1, etc. to D:\system.img  
`SparseConverter.exe /d[ecompress] E:\system.new.dat D:\system.img E:\system.transfer.list`  
will decompress E:\system.new.dat to D:\system.img using instructions in E:\system.transfer.list

## Credits

[@TalAloni](https://github.com/TalAloni) Original Author  
[@xpirt](https://github.com/xpirt) for the sparse Android data image decompression [algorithm](https://github.com/xpirt/sdat2img/blob/1b08432247fce8037fd6a43685c6e7037a2e553a/sdat2img.py)