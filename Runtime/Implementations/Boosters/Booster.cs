namespace SwiftFramework.Core.Boosters
{
    [System.Serializable]
    public class Booster
    {
        public BoosterConfigLink link;
        public Link context;

        public long expirationTime;

        public Booster(BoosterConfigLink link, long expirationTime, Link context)
        {
            this.link = link;
            this.context = context;
            this.expirationTime = expirationTime;
        }

        public Booster()
        {

        }
    }
}
