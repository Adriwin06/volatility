using Volatility.Utilities;

namespace Volatility.Resources;

public class GuiPopup : Resource
{
    private const int HeaderSize = 0x8;
    private const int PopupStructSize = 0xC0;

    public List<Popup> Popups { get; } = [];

    public override ResourceType ResourceType => ResourceType.GuiPopup;
    public override Platform ResourcePlatform => Platform.Agnostic;

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness)
    {
        base.WriteToStream(writer, endianness);

        long start = writer.BaseStream.Position;
        writer.Write((uint)HeaderSize);
        writer.Write((short)Popups.Count);
        writer.Write((short)PopupStructSize);
        writer.WriteSection(start + HeaderSize, Popups, Popup.Write);
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness)
    {
        base.ParseFromStream(reader, endianness);

        Popups.Clear();

        long start = reader.BaseStream.Position;
        uint dataPtr = reader.ReadUInt32();
        short count = reader.ReadInt16();
        short elemSize = reader.ReadInt16();

        if (elemSize != PopupStructSize)
        {
            throw new InvalidDataException(
                $"GuiPopup element size mismatch! Expected 0x{PopupStructSize:X}, found 0x{elemSize:X}.");
        }

        reader.ParseSection(start + dataPtr, count, Popup.Read, Popups);
    }

    public GuiPopup() : base() { }

    public GuiPopup(string path, Endian endianness) : base(path, endianness) { }

    public enum PopupStyle : int
    {
        E_POPUPSTYLE_CRASHNAV_WAIT = 0,
        E_POPUPSTYLE_CRASHNAV_OK = 1,
        E_POPUPSTYLE_CRASHNAV_OKCANCEL = 2,
        E_POPUPSTYLE_CRASHNAV_ONLINE_WAIT = 3,
        E_POPUPSTYLE_CRASHNAV_ONLINE_OK = 4,
        E_POPUPSTYLE_CRASHNAV_ONLINE_OKCANCEL = 5,
        E_POPUPSTYLE_INGAME_WAIT = 6,
        E_POPUPSTYLE_INGAME_OK = 7,
        E_POPUPSTYLE_INGAME_OKCANCEL = 8,
        E_POPUPSTYLE_INGAME_ONLINE_WAIT = 9,
        E_POPUPSTYLE_INGAME_ONLINE_OK = 10,
        E_POPUPSTYLE_INGAME_ONLINE_OKCANCEL = 11,
        E_POPUPSTYLE_INGAME_ONLINE_ENTER_FREEBURN = 12,
        E_POPUPSTYLE_CUSTOM = 13,
        E_POPUPSTYLE_ISLAND_ENTER = 14,
        E_POPUPSTYLE_ISLAND_BUY = 15,
        E_POPUPSTYLE_COUNT = 16
    }

    public enum PopupIcons : int
    {
        E_POPUPICONS_INVISIBLE = 0,
        E_POPUPICONS_WARNING = 1,
        E_POPUPICONS_COUNT = 2
    }

    public enum PopupParamTypes : int
    {
        E_POPUPPARAMTYPES_UNUSED = 0,
        E_POPUPPARAMTYPES_STRING = 1,
        E_POPUPPARAMTYPES_STRING_ID = 2,
        E_POPUPPARAMTYPES_COUNT = 3
    }

    public struct Popup
    {
        public CgsID NameId;
        public string Name;
        public PopupStyle Style;
        public PopupIcons Icon;
        public string TitleId;
        public string MessageId;
        public PopupParamTypes MessageParam0;
        public PopupParamTypes MessageParam1;
        public int MessageParamsUsed;
        public string Button1Id;
        public PopupParamTypes Button1Param;
        public bool Button1ParamUsed;
        public string Button2Id;
        public PopupParamTypes Button2Param;
        public bool Button2ParamUsed;

        public static Popup Read(ResourceBinaryReader reader)
        {
            Popup popup = new()
            {
                NameId = reader.ReadUInt64(),
                Name = ResourceUtilities.ReadFixedString(reader, 13)
            };

            reader.BaseStream.Seek(0x3, SeekOrigin.Current);

            popup.Style = (PopupStyle)reader.ReadInt32();
            popup.Icon = (PopupIcons)reader.ReadInt32();
            popup.TitleId = ResourceUtilities.ReadFixedString(reader, 32);
            popup.MessageId = ResourceUtilities.ReadFixedString(reader, 32);
            popup.MessageParam0 = (PopupParamTypes)reader.ReadInt32();
            popup.MessageParam1 = (PopupParamTypes)reader.ReadInt32();
            popup.MessageParamsUsed = reader.ReadInt32();
            popup.Button1Id = ResourceUtilities.ReadFixedString(reader, 32);
            popup.Button1Param = (PopupParamTypes)reader.ReadInt32();
            popup.Button1ParamUsed = reader.ReadByte() != 0;
            popup.Button2Id = ResourceUtilities.ReadFixedString(reader, 32);

            reader.BaseStream.Seek(0x3, SeekOrigin.Current);

            popup.Button2Param = (PopupParamTypes)reader.ReadInt32();
            popup.Button2ParamUsed = reader.ReadByte() != 0;

            reader.BaseStream.Seek(0x7, SeekOrigin.Current);

            return popup;
        }

        public static void Write(ResourceBinaryWriter writer, Popup popup)
        {
            writer.Write(popup.NameId);
            ResourceUtilities.WriteFixedString(writer, popup.Name, 13);
            writer.Write(new byte[0x3]);
            writer.Write((int)popup.Style);
            writer.Write((int)popup.Icon);
            ResourceUtilities.WriteFixedString(writer, popup.TitleId, 32);
            ResourceUtilities.WriteFixedString(writer, popup.MessageId, 32);
            writer.Write((int)popup.MessageParam0);
            writer.Write((int)popup.MessageParam1);
            writer.Write(popup.MessageParamsUsed);
            ResourceUtilities.WriteFixedString(writer, popup.Button1Id, 32);
            writer.Write((int)popup.Button1Param);
            writer.Write((byte)(popup.Button1ParamUsed ? 1 : 0));
            ResourceUtilities.WriteFixedString(writer, popup.Button2Id, 32);
            writer.Write(new byte[0x3]);
            writer.Write((int)popup.Button2Param);
            writer.Write((byte)(popup.Button2ParamUsed ? 1 : 0));
            writer.Write(new byte[0x7]);
        }
    }
}
