namespace FS_Web_Stuff.Server
{
    using Google.Cloud.Firestore;

    public static class DBInteract
    {

        public static FirestoreDb db = FirestoreDb.Create("fs-web-stuff");



        public static async Task AddDocument(string collection, object data)
        {
            DocumentReference docRef = db.Collection(collection).Document();
            await docRef.SetAsync(data);
        }


    }
}
