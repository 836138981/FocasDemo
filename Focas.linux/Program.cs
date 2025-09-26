ushort loglevel= 0;
string path=AppDomain.CurrentDomain.BaseDirectory;

Focas1.cnc_startupprocess( loglevel,path);

ushort flibhndl = 0;
short ret = 0;
ret = Focas1.cnc_allclibhndl3("192.168.101.89", 8193, 10, out flibhndl);
if (ret == Focas1.EW_OK)
{
    Console.WriteLine($"Connected,handle:{flibhndl}");
    Focas1.ODBPOS fos = new Focas1.ODBPOS();
    short num = Focas1.MAX_AXIS;
    short type = -1; ;
    ret = Focas1.cnc_rdposition(flibhndl, type, ref num, fos);
    if (ret == Focas1.EW_OK)
    {
        Console.WriteLine(fos.p1.rel.name.ToString() + ":  " + fos.p1.rel.data * Math.Pow(10, -fos.p1.rel.dec));
        Console.WriteLine(fos.p2.rel.name.ToString() + ":  " + fos.p2.rel.data * Math.Pow(10, -fos.p2.rel.dec));
        Console.WriteLine(fos.p3.rel.name.ToString() + ":  " + fos.p3.rel.data * Math.Pow(10, -fos.p3.rel.dec));
        Console.WriteLine(fos.p1.dist.name.ToString() + ":  " + fos.p1.dist.data * Math.Pow(10, -fos.p1.dist.dec));
        Console.WriteLine(fos.p2.dist.name.ToString() + ":  " + fos.p2.dist.data * Math.Pow(10, -fos.p2.dist.dec));
        Console.WriteLine(fos.p3.dist.name.ToString() + ":  " + fos.p3.dist.data * Math.Pow(10, -fos.p3.dist.dec));
    }
    else
    {
        Console.WriteLine($"cnc_rdposition failed,ret:{ret}");
    }
}
else
{
    Console.WriteLine("Can't Connect");
}

Focas1.cnc_exitthread();
Focas1.cnc_exitprocess();