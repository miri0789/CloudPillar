﻿
namespace Shared.Entities.Twin;

public class TwinDesired
{
   public string? ChangeSign { get; set; }

   public TwinChangeSpec? ChangeSpec { get; set; }
   public List<TwinChangeSpec>? ChangeSpecList { get; set; }
   
}
