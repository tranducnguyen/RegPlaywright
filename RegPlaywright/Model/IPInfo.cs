using LiteDB;

namespace RegPlaywright.Model
{
    class IPInfo
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string IP { get; set; }
        public string CheckPoint { get; set; }
        public string Success { get; set; }
    }
}
