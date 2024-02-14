# Clarity.Image
APNG and PNG library for C# .net.

C#でapngを扱うライブラリおよび使用サンプルです。  
pngおよびapngファイルを読み込み、RGBA形式のバッファを作成して保持します。  
apngの読み込みを書いていたら自然とpngの方も出来ちゃったので含めてますが、pngはおまけみたいなもんで、普通に.net標準の方使った方が便利だと思います。

## Build
C#  
Visual Studio 2022  
.Net6  
[System.IO.Hashing-8.0.0](https://www.nuget.org/packages/System.IO.Hashing/8.0.0)

## Supported image formats
これしか対応してません。
#### 読み込み
- 8bit TrueColor 
- 8bit TrueColorWithAlpha
- 8bit IndexedColor

#### 書み込み
- 8bit TrueColorWithAlpha


## Usage
### APNG
#### Read
```
Clarity.Image.PNG.APngFile ap = new Clarity.Image.PNG.APngFile();
await ap.Load(@"anime.png");
```
#### Write
```
List<APngFrame> framelist = new List<APngFrame>();
//Set image size, delay time and rgba buffer to APngFrame 
APngWriter aw = new APngWriter();
await aw.Save("save_anime.png", framelist);
```



### PNG
#### Read
```
Clarity.Image.PNG.PngFile png = new Clarity.Image.PNG.PngFile();
awit png.Load(@"image.png");
```

#### Write
```

```
#### 
## Author 
sulkmqul  
[Blog](http://blog.livedoor.jp/serialpath/)


## LICENSE
[WTFPL](http://www.wtfpl.net/)  

