using LiteDB;

namespace RegPlaywright.Model
{
    class UAList
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string UA { get; set; }
        public string CheckPoint { get; set; }
        public string Success { get; set; }
    }
}
