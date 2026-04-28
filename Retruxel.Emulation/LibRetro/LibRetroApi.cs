using System;
using System.Runtime.InteropServices;

namespace Retruxel.Emulation.LibRetro;

public static class LibRetroApi
{
    public const uint API_VERSION = 1;
}

public enum RetroPixelFormat : uint
{
    RETRO_PIXEL_FORMAT_0RGB1555 = 0,
    RETRO_PIXEL_FORMAT_XRGB8888 = 1,
    RETRO_PIXEL_FORMAT_RGB565 = 2
}

public enum RetroDeviceType : uint
{
    RETRO_DEVICE_NONE = 0,
    RETRO_DEVICE_JOYPAD = 1,
    RETRO_DEVICE_MOUSE = 2,
    RETRO_DEVICE_KEYBOARD = 3,
    RETRO_DEVICE_LIGHTGUN = 4,
    RETRO_DEVICE_ANALOG = 5,
    RETRO_DEVICE_POINTER = 6
}

public enum RetroDeviceIdJoypad : uint
{
    RETRO_DEVICE_ID_JOYPAD_B = 0,
    RETRO_DEVICE_ID_JOYPAD_Y = 1,
    RETRO_DEVICE_ID_JOYPAD_SELECT = 2,
    RETRO_DEVICE_ID_JOYPAD_START = 3,
    RETRO_DEVICE_ID_JOYPAD_UP = 4,
    RETRO_DEVICE_ID_JOYPAD_DOWN = 5,
    RETRO_DEVICE_ID_JOYPAD_LEFT = 6,
    RETRO_DEVICE_ID_JOYPAD_RIGHT = 7,
    RETRO_DEVICE_ID_JOYPAD_A = 8,
    RETRO_DEVICE_ID_JOYPAD_X = 9,
    RETRO_DEVICE_ID_JOYPAD_L = 10,
    RETRO_DEVICE_ID_JOYPAD_R = 11,
    RETRO_DEVICE_ID_JOYPAD_L2 = 12,
    RETRO_DEVICE_ID_JOYPAD_R2 = 13,
    RETRO_DEVICE_ID_JOYPAD_L3 = 14,
    RETRO_DEVICE_ID_JOYPAD_R3 = 15
}

public enum RetroEnvironment : uint
{
    RETRO_ENVIRONMENT_SET_ROTATION = 1,
    RETRO_ENVIRONMENT_GET_OVERSCAN = 2,
    RETRO_ENVIRONMENT_GET_CAN_DUPE = 3,
    RETRO_ENVIRONMENT_SET_MESSAGE = 6,
    RETRO_ENVIRONMENT_SHUTDOWN = 7,
    RETRO_ENVIRONMENT_SET_PERFORMANCE_LEVEL = 8,
    RETRO_ENVIRONMENT_GET_SYSTEM_DIRECTORY = 9,
    RETRO_ENVIRONMENT_SET_PIXEL_FORMAT = 10,
    RETRO_ENVIRONMENT_SET_INPUT_DESCRIPTORS = 11,
    RETRO_ENVIRONMENT_SET_KEYBOARD_CALLBACK = 12,
    RETRO_ENVIRONMENT_SET_DISK_CONTROL_INTERFACE = 13,
    RETRO_ENVIRONMENT_SET_HW_RENDER = 14,
    RETRO_ENVIRONMENT_GET_VARIABLE = 15,
    RETRO_ENVIRONMENT_SET_VARIABLES = 16,
    RETRO_ENVIRONMENT_GET_VARIABLE_UPDATE = 17,
    RETRO_ENVIRONMENT_SET_SUPPORT_NO_GAME = 18,
    RETRO_ENVIRONMENT_GET_LIBRETRO_PATH = 19,
    RETRO_ENVIRONMENT_SET_FRAME_TIME_CALLBACK = 21,
    RETRO_ENVIRONMENT_SET_AUDIO_CALLBACK = 22,
    RETRO_ENVIRONMENT_GET_RUMBLE_INTERFACE = 23,
    RETRO_ENVIRONMENT_GET_INPUT_DEVICE_CAPABILITIES = 24,
    RETRO_ENVIRONMENT_GET_SENSOR_INTERFACE = 25,
    RETRO_ENVIRONMENT_GET_CAMERA_INTERFACE = 26,
    RETRO_ENVIRONMENT_GET_LOG_INTERFACE = 27,
    RETRO_ENVIRONMENT_GET_PERF_INTERFACE = 28,
    RETRO_ENVIRONMENT_GET_LOCATION_INTERFACE = 29,
    RETRO_ENVIRONMENT_GET_CONTENT_DIRECTORY = 30,
    RETRO_ENVIRONMENT_GET_SAVE_DIRECTORY = 31,
    RETRO_ENVIRONMENT_SET_SYSTEM_AV_INFO = 32,
    RETRO_ENVIRONMENT_SET_PROC_ADDRESS_CALLBACK = 33,
    RETRO_ENVIRONMENT_SET_SUBSYSTEM_INFO = 34,
    RETRO_ENVIRONMENT_SET_CONTROLLER_INFO = 35,
    RETRO_ENVIRONMENT_SET_MEMORY_MAPS = 36,
    RETRO_ENVIRONMENT_SET_GEOMETRY = 37,
    RETRO_ENVIRONMENT_GET_USERNAME = 38,
    RETRO_ENVIRONMENT_GET_LANGUAGE = 39
}

public enum RetroMemory : uint
{
    RETRO_MEMORY_SAVE_RAM = 0,
    RETRO_MEMORY_RTC = 1,
    RETRO_MEMORY_SYSTEM_RAM = 2,
    RETRO_MEMORY_VIDEO_RAM = 3
}

[StructLayout(LayoutKind.Sequential)]
public struct RetroGameInfo
{
    public IntPtr path;
    public IntPtr data;
    public UIntPtr size;
    public IntPtr meta;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct RetroSystemInfo
{
    public IntPtr library_name;
    public IntPtr library_version;
    public IntPtr valid_extensions;
    [MarshalAs(UnmanagedType.U1)]
    public bool need_fullpath;
    [MarshalAs(UnmanagedType.U1)]
    public bool block_extract;
}

[StructLayout(LayoutKind.Sequential)]
public struct RetroSystemAvInfo
{
    public RetroGameGeometry geometry;
    public RetroSystemTiming timing;
}

[StructLayout(LayoutKind.Sequential)]
public struct RetroGameGeometry
{
    public uint base_width;
    public uint base_height;
    public uint max_width;
    public uint max_height;
    public float aspect_ratio;
}

[StructLayout(LayoutKind.Sequential)]
public struct RetroSystemTiming
{
    public double fps;
    public double sample_rate;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void RetroVideoRefreshCallback(IntPtr data, uint width, uint height, UIntPtr pitch);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void RetroAudioSampleCallback(short left, short right);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate UIntPtr RetroAudioSampleBatchCallback(IntPtr data, UIntPtr frames);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void RetroInputPollCallback();

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate short RetroInputStateCallback(uint port, uint device, uint index, uint id);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: MarshalAs(UnmanagedType.U1)]
public delegate bool RetroEnvironmentCallback(uint cmd, IntPtr data);
