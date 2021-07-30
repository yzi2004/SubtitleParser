namespace SubtitleParser.Sup
{
    public enum SegType : ushort
    {
        PDS = 0x14,
        ODS = 0x15,
        PCS = 0x16,
        WDS = 0x17,
        END = 0x80
    }

    public enum CompState : ushort
    {
        Normal = 0x00,
        AcquisitionPoint = 0x40,
        EpochStart = 0x80
    }
}
