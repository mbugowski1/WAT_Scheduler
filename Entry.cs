namespace WAT_Planner
{
    class Entry
    {
        public string shortname;
        public string longname;
        public string leader;
        public string room;
        public string type;
        public string shortType;
        public System.DateTime start;
        public System.DateTime stop;
        public int timeIndex;

        public override string ToString()
        {
            return $"{{ {longname} - {leader} ({type}) {start}}}";
        }
    }
}