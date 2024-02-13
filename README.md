# Clarity.Image.PNG
PNG and APNG library for C# .net.


C#でapngを扱うライブラリおよび使用サンプルです。  
pngおよびapngファイルを読み込み、RGBA形式のバッファを作成して保持します。


## Build
C#  
Visual Studio 2022  
.Net6  
[System.IO.Hashing-8.0.0](https://www.nuget.org/packages/System.IO.Hashing/8.0.0)


## Usage
PNG
```
using Clarity.Image.PNG;

//load png file
Clarity.Image.PNG.PngFile png = new Clarity.Image.PNG.PngFile();
awit png.Load(@"image.png");
```

APNG
```
using Clarity.Image.PNG;

Clarity.Image.PNG.APngFile ap = new Clarity.Image.PNG.APngFile();
await ap.Load(@"anime.png");
```

## Author 
sulkmqul  
[Blog](http://blog.livedoor.jp/serialpath/)


## LICENSE
[WTFPL](http://www.wtfpl.net/)  

