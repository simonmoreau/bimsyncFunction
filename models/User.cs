namespace bimsyncFunction
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string bimsync_id {get;set;}
        public string PowerBiSecret{get;set;}
        public string AccessToken{get;set;}
        public string TokenType{get;set;}
        public int? TokenExpireIn{get;set;}
        public string RefreshToken{get;set;}
        public System.DateTime RefreshDate{get;set;}
        public string BCFToken{get;set;}
    }
}
