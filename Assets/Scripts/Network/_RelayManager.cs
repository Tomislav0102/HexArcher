using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
public class RelayManager
{
    int _maxPlayers;
    public RelayManager(int maxPlayers)
    {
        _maxPlayers = maxPlayers;
    }
    
    public async Task<Allocation> AllocateRelay() //called by host
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(_maxPlayers - 1); //host is excluded
            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to allocate relay: " + e.Message);
            return default;
        }
    }
    
    public async Task<string> GetRelayJoinCode(Allocation allocation) //called by host
    {
        try
        {
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to get relay join code: " + e.Message);
            return default;
        }
    }
    
    public async Task<JoinAllocation> JoinRelay(string relayJoinCode) //called by client
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            return joinAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to join relay: " + e.Message);
            return default;
        }
    } 
}
public struct RelayHostData
{
    public string JoinCode;
    public string IPv4Address;
    public ushort Port;
    public System.Guid AllocationID;
    public byte[] AllocationIDBytes;
    public byte[] ConnectionData;
    public byte[] Key;
}
public struct RelayJoinData
{
    public string JoinCode;
    public string IPv4Address;
    public ushort Port;
    public System.Guid AllocationID;
    public byte[] AllocationIDBytes;
    public byte[] ConnectionData;
    public byte[] HostConnectionData;
    public byte[] Key;
}