using LiteDB;

namespace RegPlaywright.Model
{
    class PhoneList
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string Phone { get; set; }
        public string Active { get; set; }
    }
}
