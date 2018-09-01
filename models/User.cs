namespace bimsyncFunction
{
    public class User
    {
        public string id { get; set; }
        public string Name { get; set; }
        public string PowerBISecret {get;set;}
        public bimsync.AccessToken AccessToken{get;set;}
        public System.DateTime RefreshDate{get;set;}
        public string BCFToken{get;set;}
    }

    public class AuthorisationCode
    {
        public string AuthorizationCode {get;set;}
        public string RedirectURI {get;set;}
    }
}
