namespace UsageAssessmentWithCecil
{
    partial class Program
    {
        public class ReferenceInfo
        {
            public ReferenceInfo(string moduleFileName, string methodCall)
            {
                this.MethodCall = methodCall;
                this.ModuleFileName = moduleFileName;
            }
            public string ModuleFileName { get; }
            public string MethodCall { get; }
            public override bool Equals(object obj)
            {
                if (obj is ReferenceInfo other)
                {

                    return other.ModuleFileName == this.ModuleFileName && this.MethodCall == this.MethodCall;
                }
                return false;
            }
            public override int GetHashCode()
            {
                return ((ModuleFileName ?? string.Empty) + (MethodCall ?? string.Empty)).GetHashCode();
            }
        }
    }
}
