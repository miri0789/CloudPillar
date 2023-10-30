namespace CloudPillar.Agent.Wrappers;
public interface ISHA256Wrapper
{
    byte[] ComputeHash(byte[] input);
}