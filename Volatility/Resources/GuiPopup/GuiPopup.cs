using Volatility.Utilities;

namespace Volatility.Resources;

public class GuiPopup : TypedResource
{
    private const int HeaderSize = 0x40;
    private const int PopupStructSize = 0xC0;
    private const int PopupOffsetEntrySize = sizeof(uint);
    private const int HeaderAlignment = 0x40;

    public List<Popup> Popups { get; set; } = [];

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness)
    {
        base.WriteToStream(writer, endianness);

        uint count = (uint)Popups.Count;
        uint popupOffsetsStart = HeaderSize;
        uint firstPopupOffset = AlignOffset(popupOffsetsStart + (count * PopupOffsetEntrySize), HeaderAlignment);
        uint totalSize = firstPopupOffset + (count * PopupStructSize);

        writer.Write(popupOffsetsStart);
        writer.Write((short)Popups.Count);
        writer.Write((short)totalSize);
        writer.Write(new byte[HeaderSize - 0x8]);

        for (int i = 0; i < Popups.Count; i++)
        {
            writer.Write(firstPopupOffset + (uint)(i * PopupStructSize));
        }

        PaddingUtilities.WritePadding(writer.BaseStream, HeaderAlignment);

        for (int i = 0; i < Popups.Count; i++)
        {
            writer.BaseStream.Position = firstPopupOffset + (i * PopupStructSize);
            Popup.Write(writer, Popups[i]);
        }
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness)
    {
        base.ParseFromStream(reader, endianness);

        Popups.Clear();

        uint dataPtr = reader.ReadUInt32();
        short count = reader.ReadInt16();
        short totalSize = reader.ReadInt16();

        if (dataPtr < HeaderSize)
        {
            throw new InvalidDataException(
                $"GuiPopup data pointer mismatch! Expected >= 0x{HeaderSize:X}, found 0x{dataPtr:X}.");
        }

        long expectedMinimumSize = dataPtr + (count * PopupOffsetEntrySize);
        if (reader.BaseStream.Length < expectedMinimumSize)
        {
            throw new InvalidDataException(
                $"GuiPopup offset table exceeds file length. Needed 0x{expectedMinimumSize:X}, found 0x{reader.BaseStream.Length:X}.");
        }

        List<uint> popupOffsets = reader.ParseSection(dataPtr, count, r => r.ReadUInt32());
        for (int i = 0; i < popupOffsets.Count; i++)
        {
            uint popupOffset = popupOffsets[i];
            if (popupOffset == 0)
            {
                continue;
            }

            if (popupOffset + PopupStructSize > reader.BaseStream.Length)
            {
                throw new InvalidDataException(
                    $"GuiPopup entry {i} at 0x{popupOffset:X} exceeds file length 0x{reader.BaseStream.Length:X}.");
            }

            reader.ParseSection(popupOffset, Popup.Read, out Popup popup);
            Popups.Add(popup);
        }

        if (totalSize > 0 && totalSize != reader.BaseStream.Length)
        {
            Console.WriteLine($"WARNING: GuiPopup reported size 0x{totalSize:X}, actual size 0x{reader.BaseStream.Length:X}.");
        }
    }

    public GuiPopup() : base(ResourceType.GuiPopup) { }

    public GuiPopup(string path, Endian endianness)
        : base(ResourceType.GuiPopup, path, endianness) { }

    private static uint AlignOffset(uint value, uint alignment)
    {
        uint remainder = value % alignment;
        return remainder == 0 ? value : value + (alignment - remainder);
    }

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
        [EditorHidden]
        public byte[] NamePaddingBytes;
        [EditorHidden]
        public byte[] Button2PaddingBytes;
        [EditorHidden]
        public byte[] TrailingBytes;

        public static Popup Read(ResourceBinaryReader reader)
        {
            Popup popup = new()
            {
                NameId = reader.ReadUInt64(),
                Name = ResourceUtilities.ReadFixedString(reader, 13),
                NamePaddingBytes = new byte[0x3],
                Button2PaddingBytes = new byte[0x3],
                TrailingBytes = new byte[0x7]
            };

            popup.NamePaddingBytes = reader.ReadBytes(0x3);

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
            popup.Button2PaddingBytes = reader.ReadBytes(0x3);

            popup.Button2Param = (PopupParamTypes)reader.ReadInt32();
            popup.Button2ParamUsed = reader.ReadByte() != 0;
            popup.TrailingBytes = reader.ReadBytes(0x7);

            return popup;
        }

        public static void Write(ResourceBinaryWriter writer, Popup popup)
        {
            writer.Write(popup.NameId);
            ResourceUtilities.WriteFixedString(writer, popup.Name, 13);
            writer.WriteFixedBytes(popup.NamePaddingBytes, 0x3);
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
            writer.WriteFixedBytes(popup.Button2PaddingBytes, 0x3);
            writer.Write((int)popup.Button2Param);
            writer.Write((byte)(popup.Button2ParamUsed ? 1 : 0));
            writer.WriteFixedBytes(popup.TrailingBytes, 0x7);
        }
    }
}
