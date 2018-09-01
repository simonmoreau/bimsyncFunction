using System;

namespace bimsyncFunction.bimsync
{
    public class Project
    {
        public DateTime createdAt { get; set; }
        public string description { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public DateTime updatedAt { get; set; }
    }

    public class Model
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class User
    {
        public DateTime createdAt { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string username { get; set; }
    }

    public class Revision
    {
        public string comment { get; set; }
        public DateTime createdAt { get; set; }
        public string id { get; set; }
        public Model model { get; set; }
        public User user { get; set; }
        public int version { get; set; }
    }

    public class Member
    {
        public string role { get; set; }
        public User user { get; set; }
    }

    public class AccessToken
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
    }
    public class BCFToken
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
    }

    public class ViewerToken
    {
        public string token {get;set;}
        public string url {get;set;}
    }

}