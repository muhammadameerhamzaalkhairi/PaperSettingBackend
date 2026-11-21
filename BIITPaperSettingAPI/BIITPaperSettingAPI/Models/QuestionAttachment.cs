using System;
using System.Collections.Generic;

namespace BIITPaperSettingAPI.Models;

public partial class QuestionAttachment
{
    public int AttachmentId { get; set; }

    public int QuestionVersionId { get; set; }

    public int? QuestionPartId { get; set; }

    public int FileAssetId { get; set; }

    public int DisplayOrder { get; set; }

    public virtual FileAsset FileAsset { get; set; } = null!;

    public virtual QuestionPart? QuestionPart { get; set; }

    public virtual QuestionPart? QuestionPartNavigation { get; set; }

    public virtual QuestionVersion QuestionVersion { get; set; } = null!;
}
