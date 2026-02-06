using FileTypeDetection;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: FileClassifier.App <path>");
    return 2;
}

var path = args[0];
var detector = new FileTypeDetector();
var t = detector.Detect(path);
Console.WriteLine(t.Kind);
return 0;