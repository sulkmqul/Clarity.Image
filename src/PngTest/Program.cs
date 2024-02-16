using Clarity.Image.PNG;
using PngTest;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;



//APNGの読込および解析
async Task LoadAPngFile(string apng_filepath)
{   
    
    //読込
    Clarity.Image.PNG.APngFile ap = new Clarity.Image.PNG.APngFile();
    await ap.Load(apng_filepath);

    //もろもろ表示
    Console.WriteLine($"num of frames:{ap.FrameCount}");
    Console.WriteLine($"image width:{ap.Width}");
    Console.WriteLine($"image Height:{ap.Height}");

    int i = 0;
    foreach (var a in ap.FrameList)
    {
        Console.WriteLine($"Frame:{i} Width:{a.Width} Height:{a.Height} delay time={a.Time} BufferSize:{a.FrameData.Length}");
        
        //Create Frame Bitmap Image
        //Bitmap bit = a.CreateBitmapImage();
        i++;
    }
}

//APNGファイルの作成
async Task WriteAPng(string wfilepath)
{
    List<APngFrame> flist = new List<APngFrame>();
    //フレームの作成
    {
        //Bitmap to RGBA
        //Bitmap bit = new Bitmap("test.bmp");
        //byte[] buf = BitmapSupporter.CreateRGBABuffer(bit);

        //
        //APngFrame frame = new APngFrame(bit.Width, bit.Height, 100, buf);
        //flist.Add(frame);
    }

    //書き込み
    APngWriter aw = new APngWriter();
    await aw.Save(wfilepath, flist);
    
}


//pngファイルの使用法
async Task PngTest(string rfilepath, string wfilepath)
{
    //読込
    Clarity.Image.PNG.PngFile png = new Clarity.Image.PNG.PngFile();
    await png.Load(rfilepath);

    //表示
    Console.WriteLine($"image width:{png.Width}");
    Console.WriteLine($"image height:{png.Height}");
    Console.WriteLine($"image buffer size:{png.Data.Length}");

    //書き込み
    Clarity.Image.PNG.PngWriter pw = new Clarity.Image.PNG.PngWriter();
    await pw.Save(wfilepath, png.Width, png.Height, png.Data);
}

try
{

    //apng読込
    await LoadAPngFile(@"reada.png");

    //apng書き込み
    //await WriteAPng(@"writea.png");

    //png読み書き
    //await PngTest("read.png", "write.png");


}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());   
}


