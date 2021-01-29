﻿// Copyright (c) micah686. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using LiteDB;

namespace VnManager.Models.Db.Vndb.Main
{
    public class VnInfoRelations
    {
        [BsonId]
        public int Index { get; set; }
        public uint VnId { get; set; }
        public int? RelationId { get; set; }
        public string Relation { get; set; }
        public string Title { get; set; }
        public string Original { get; set; }
        public string Official { get; set; }
    }
}
