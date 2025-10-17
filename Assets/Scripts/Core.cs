using System.Runtime.InteropServices;

public class Core
{
    public const string version = "0-1-0-00078";

#if UNITY_WEBGL
    public const string libName = "__Internal";
#else
#if UNITY_ANDROID
    public const string target = "android";
#elif UNITY_IOS
    public const string target = "ios";
#elif UNITY_STANDALONE_OSX
    public const string target = "macos";
#elif UNITY_STANDALONE_WIN
    public const string target = "windows";
#else
    public const string target = "native";
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public const string mode = "debug";
#else
    public const string mode = "release";
#endif

    public const string libName = "core_" + target + "_" + mode + "_" + version;
#endif

    [DllImport(libName)]
    public static extern int add(int left, int right);
}
