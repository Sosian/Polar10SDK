namespace HumanMusicController.Bluetooth
{
    public abstract class BleGattBase
    {
        public static readonly int DEFAULT_ATT_MTU_SIZE = 23;
        private static readonly int DEFAULT_MTU_SIZE = DEFAULT_ATT_MTU_SIZE - 3;

        /**
        * Characteristic properties
        */
        public static readonly int PROPERTY_BROADCAST = 0x01;
        public static readonly int PROPERTY_READ = 0x02;
        public static readonly int PROPERTY_WRITE_NO_RESPONSE = 0x04;
        public static readonly int PROPERTY_WRITE = 0x08;
        public static readonly int PROPERTY_NOTIFY = 0x10;
        public static readonly int PROPERTY_INDICATE = 0x20;
        public static readonly int PROPERTY_SIGNED_WRITE = 0x40;
        public static readonly int PROPERTY_EXTENDED_PROPS = 0x80;

        /**
        * Permissions, note only in service role
        */
        public static readonly int PERMISSION_READ = 0x01;
        public static readonly int PERMISSION_READ_ENCRYPTED = 0x02;
        public static readonly int PERMISSION_READ_ENCRYPTED_MITM = 0x04;
        public static readonly int PERMISSION_WRITE = 0x10;
        public static readonly int PERMISSION_WRITE_ENCRYPTED = 0x20;
        public static readonly int PERMISSION_WRITE_ENCRYPTED_MITM = 0x40;
        public static readonly int PERMISSION_WRITE_SIGNED = 0x80;

        /**
        * ATT ERROR CODES, endpoint shall prefer these error codes when calling gatt client callbacks
        */
        public static readonly int ATT_SUCCESS = 0;
        public static readonly int ATT_INVALID_HANDLE = 0x1;
        public static readonly int ATT_READ_NOT_PERMITTED = 0x2;
        public static readonly int ATT_WRITE_NOT_PERMITTED = 0x3;
        public static readonly int ATT_INVALID_PDU = 0x4;
        public static readonly int ATT_INSUFFICIENT_AUTHENTICATION = 0x5;
        public static readonly int ATT_REQUEST_NOT_SUPPORTED = 0x6;
        public static readonly int ATT_INVALID_OFFSET = 0x7;
        public static readonly int ATT_INSUFFICIENT_AUTHOR = 0x8;
        public static readonly int ATT_PREPARE_QUEUE_FULL = 0x9;
        public static readonly int ATT_ATTR_NOT_FOUND = 0xa;
        public static readonly int ATT_ATTR_NOT_LONG = 0xb;
        public static readonly int ATT_INSUFFICIENT_KEY_SIZE = 0xc;
        public static readonly int ATT_INVALID_ATTRIBUTE_LENGTH = 0xd;
        public static readonly int ATT_UNLIKELY = 0xe;
        public static readonly int ATT_INSUFFICIENT_ENCRYPTION = 0xf;
        public static readonly int ATT_UNSUPPORTED_GRP_TYPE = 0x10;
        public static readonly int ATT_INSUFFICIENT_RESOURCES = 0x11;
        public static readonly int ATT_NOTIFY_OR_INDICATE_OFF = 0xff;
        //0x80-0x9F Application Errors
        //0xA0-0xDF Reserved for future use
        //0xE0-0xFF Common Profile and Service Error Codes
        //          Defined in Core Specification Supplement Part B.
        public static readonly int ATT_WRITE_REQUEST_REJECTED = 0xFC;
        public static readonly int ATT_CCCD_IMPROPERLY_CONFIGURED = 0xFD;
        public static readonly int ATT_PROCEDURE_ALREADY_IN_PROGRESS = 0xFE;
        public static readonly int ATT_OUT_OF_RANGE = 0xFF;

        public static readonly int ATT_UNKNOWN_ERROR = 0x100;
    }
}