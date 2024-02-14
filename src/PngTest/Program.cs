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

try
{

    //読込
    await LoadAPngFile(@"read.png");
    //書き込み
    await WriteAPng(@"write.png");

   

}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());   
}


