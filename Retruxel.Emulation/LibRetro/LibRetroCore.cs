using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Retruxel.Emulation.LibRetro;

public class LibRetroCore : IDisposable
{
    private IntPtr _coreHandle;
    private string? _corePath;
    
    private RetroVideoRefreshCallback? _videoCallback;
    private RetroAudioSampleCallback? _audioCallback;
    private RetroAudioSampleBatchCallback? _audioBatchCallback;
    private RetroInputPollCallback? _inputPollCallback;
    private RetroInputStateCallback? _inputStateCallback;
    private RetroEnvironmentCallback? _environmentCallback;

    public event Action<IntPtr, uint, uint, UIntPtr>? OnVideoRefresh;
    public event Action<short, short>? OnAudioSample;
    public event Action? OnInputPoll;
    
    public RetroSystemInfo SystemInfo { get; private set; }
    public RetroSystemAvInfo AvInfo { get; private set; }
    public bool IsLoaded => _coreHandle != IntPtr.Zero;

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    private delegate void RetroInitDelegate();
    private delegate void RetroDeinitDelegate();
    private delegate uint RetroApiVersionDelegate();
    private delegate void RetroGetSystemInfoDelegate(out RetroSystemInfo info);
    private delegate void RetroGetSystemAvInfoDelegate(out RetroSystemAvInfo info);
    private delegate void RetroSetEnvironmentDelegate(RetroEnvironmentCallback callback);
    private delegate void RetroSetVideoRefreshDelegate(RetroVideoRefreshCallback callback);
    private delegate void RetroSetAudioSampleDelegate(RetroAudioSampleCallback callback);
    private delegate void RetroSetAudioSampleBatchDelegate(RetroAudioSampleBatchCallback callback);
    private delegate void RetroSetInputPollDelegate(RetroInputPollCallback callback);
    private delegate void RetroSetInputStateDelegate(RetroInputStateCallback callback);
    [return: MarshalAs(UnmanagedType.U1)]
    private delegate bool RetroLoadGameDelegate(ref RetroGameInfo game);
    private delegate void RetroUnloadGameDelegate();
    private delegate void RetroRunDelegate();
    private delegate void RetroResetDelegate();
    private delegate IntPtr RetroGetMemoryDataDelegate(uint id);
    private delegate UIntPtr RetroGetMemorySizeDelegate(uint id);

    private RetroInitDelegate? _retro_init;
    private RetroDeinitDelegate? _retro_deinit;
    private RetroApiVersionDelegate? _retro_api_version;
    private RetroGetSystemInfoDelegate? _retro_get_system_info;
    private RetroGetSystemAvInfoDelegate? _retro_get_system_av_info;
    private RetroSetEnvironmentDelegate? _retro_set_environment;
    private RetroSetVideoRefreshDelegate? _retro_set_video_refresh;
    private RetroSetAudioSampleDelegate? _retro_set_audio_sample;
    private RetroSetAudioSampleBatchDelegate? _retro_set_audio_sample_batch;
    private RetroSetInputPollDelegate? _retro_set_input_poll;
    private RetroSetInputStateDelegate? _retro_set_input_state;
    private RetroLoadGameDelegate? _retro_load_game;
    private RetroUnloadGameDelegate? _retro_unload_game;
    private RetroRunDelegate? _retro_run;
    private RetroResetDelegate? _retro_reset;
    private RetroGetMemoryDataDelegate? _retro_get_memory_data;
    private RetroGetMemorySizeDelegate? _retro_get_memory_size;

    public bool LoadCore(string corePath)
    {
        if (!File.Exists(corePath))
            return false;

        _corePath = corePath;
        _coreHandle = LoadLibrary(corePath);
        
        if (_coreHandle == IntPtr.Zero)
            return false;

        if (!LoadCoreFunctions())
        {
            FreeLibrary(_coreHandle);
            _coreHandle = IntPtr.Zero;
            return false;
        }

        SetupCallbacks();
        _retro_init?.Invoke();

        RetroSystemInfo sysInfo = default;
        _retro_get_system_info?.Invoke(out sysInfo);
        SystemInfo = sysInfo;

        return true;
    }

    private bool LoadCoreFunctions()
    {
        _retro_init = GetFunction<RetroInitDelegate>("retro_init");
        _retro_deinit = GetFunction<RetroDeinitDelegate>("retro_deinit");
        _retro_api_version = GetFunction<RetroApiVersionDelegate>("retro_api_version");
        _retro_get_system_info = GetFunction<RetroGetSystemInfoDelegate>("retro_get_system_info");
        _retro_get_system_av_info = GetFunction<RetroGetSystemAvInfoDelegate>("retro_get_system_av_info");
        _retro_set_environment = GetFunction<RetroSetEnvironmentDelegate>("retro_set_environment");
        _retro_set_video_refresh = GetFunction<RetroSetVideoRefreshDelegate>("retro_set_video_refresh");
        _retro_set_audio_sample = GetFunction<RetroSetAudioSampleDelegate>("retro_set_audio_sample");
        _retro_set_audio_sample_batch = GetFunction<RetroSetAudioSampleBatchDelegate>("retro_set_audio_sample_batch");
        _retro_set_input_poll = GetFunction<RetroSetInputPollDelegate>("retro_set_input_poll");
        _retro_set_input_state = GetFunction<RetroSetInputStateDelegate>("retro_set_input_state");
        _retro_load_game = GetFunction<RetroLoadGameDelegate>("retro_load_game");
        _retro_unload_game = GetFunction<RetroUnloadGameDelegate>("retro_unload_game");
        _retro_run = GetFunction<RetroRunDelegate>("retro_run");
        _retro_reset = GetFunction<RetroResetDelegate>("retro_reset");
        _retro_get_memory_data = GetFunction<RetroGetMemoryDataDelegate>("retro_get_memory_data");
        _retro_get_memory_size = GetFunction<RetroGetMemorySizeDelegate>("retro_get_memory_size");

        return _retro_init != null && _retro_run != null;
    }

    private T? GetFunction<T>(string name) where T : Delegate
    {
        IntPtr ptr = GetProcAddress(_coreHandle, name);
        return ptr != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<T>(ptr) : null;
    }

    private void SetupCallbacks()
    {
        _environmentCallback = EnvironmentCallback;
        _videoCallback = VideoRefreshCallback;
        _audioCallback = AudioSampleCallback;
        _audioBatchCallback = AudioSampleBatchCallback;
        _inputPollCallback = InputPollCallback;
        _inputStateCallback = InputStateCallback;

        _retro_set_environment?.Invoke(_environmentCallback);
        _retro_set_video_refresh?.Invoke(_videoCallback);
        _retro_set_audio_sample?.Invoke(_audioCallback);
        _retro_set_audio_sample_batch?.Invoke(_audioBatchCallback);
        _retro_set_input_poll?.Invoke(_inputPollCallback);
        _retro_set_input_state?.Invoke(_inputStateCallback);
    }

    public bool LoadGame(string romPath)
    {
        if (!File.Exists(romPath))
            return false;

        byte[] romData = File.ReadAllBytes(romPath);
        IntPtr dataPtr = Marshal.AllocHGlobal(romData.Length);
        Marshal.Copy(romData, 0, dataPtr, romData.Length);

        IntPtr pathPtr = Marshal.StringToHGlobalAnsi(romPath);

        var gameInfo = new RetroGameInfo
        {
            path = pathPtr,
            data = dataPtr,
            size = (UIntPtr)romData.Length,
            meta = IntPtr.Zero
        };

        bool result = _retro_load_game?.Invoke(ref gameInfo) ?? false;

        if (result)
        {
            RetroSystemAvInfo avInfo = default;
            _retro_get_system_av_info?.Invoke(out avInfo);
            AvInfo = avInfo;
        }

        Marshal.FreeHGlobal(pathPtr);
        Marshal.FreeHGlobal(dataPtr);

        return result;
    }

    public void Run() => _retro_run?.Invoke();
    public void Reset() => _retro_reset?.Invoke();

    public byte[]? GetMemory(RetroMemory memoryType)
    {
        IntPtr ptr = _retro_get_memory_data?.Invoke((uint)memoryType) ?? IntPtr.Zero;
        UIntPtr size = _retro_get_memory_size?.Invoke((uint)memoryType) ?? UIntPtr.Zero;

        if (ptr == IntPtr.Zero || size == UIntPtr.Zero)
            return null;

        byte[] data = new byte[(int)size];
        Marshal.Copy(ptr, data, 0, (int)size);
        return data;
    }

    private bool EnvironmentCallback(uint cmd, IntPtr data)
    {
        var command = (RetroEnvironment)cmd;
        
        if (command == RetroEnvironment.RETRO_ENVIRONMENT_SET_PIXEL_FORMAT)
        {
            var format = (RetroPixelFormat)Marshal.ReadInt32(data);
            return format == RetroPixelFormat.RETRO_PIXEL_FORMAT_XRGB8888;
        }
        
        return false;
    }

    private void VideoRefreshCallback(IntPtr data, uint width, uint height, UIntPtr pitch)
    {
        OnVideoRefresh?.Invoke(data, width, height, pitch);
    }

    private void AudioSampleCallback(short left, short right)
    {
        OnAudioSample?.Invoke(left, right);
    }

    private UIntPtr AudioSampleBatchCallback(IntPtr data, UIntPtr frames)
    {
        return frames;
    }

    private void InputPollCallback()
    {
        OnInputPoll?.Invoke();
    }

    private short InputStateCallback(uint port, uint device, uint index, uint id)
    {
        return 0;
    }

    public void Dispose()
    {
        if (_coreHandle != IntPtr.Zero)
        {
            _retro_unload_game?.Invoke();
            _retro_deinit?.Invoke();
            FreeLibrary(_coreHandle);
            _coreHandle = IntPtr.Zero;
        }
    }
}
