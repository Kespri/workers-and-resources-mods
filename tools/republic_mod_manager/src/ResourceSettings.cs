using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace TesmioAutoload
{
    // What the base game ships, as the editors need it: the eighteen transport
    // classes in the engine's order, and the 57 base resources with their group
    // and primary class - the donors a new resource can be cloned from.
    public static class ResourceCatalogData
    {
        public static readonly string[] TransportClasses = { "covered", "open", "gravel", "oil", "cement", "cooler", "livestock", "passanger", "concrete", "eletric", "vehicles", "general", "nuclear1", "nuclear2", "heating", "water", "sewage", "waste" };
        public sealed class Template { public string Name, Group, Transport; }
        public static readonly string[] Groups = { "basics", "build", "raw", "nuclear", "consumer", "industry", "water", "waste" };
        static Template T(string name, string group, string transport) { return new Template { Name = name, Group = group, Transport = transport }; }
        public static readonly Template[] Templates = {
            T("workers","basics","passanger"), T("eletric","basics","eletric"), T("vehicles","basics","vehicles"), T("trains","basics","vehicles"), T("heat","basics","heating"),
            T("gravel","build","gravel"), T("rawgravel","build","gravel"), T("plants","build","covered"), T("steel","build","open"), T("aluminium","build","open"), T("prefabpanels","build","open"), T("bricks","build","open"), T("wood","build","open"), T("boards","build","open"), T("asphalt","build","open"), T("concrete","build","concrete"), T("cement","build","cement"), T("alumina","build","cement"),
            T("oil","raw","oil"), T("chemicals","raw","covered"), T("coal","raw","gravel"), T("rawcoal","raw","gravel"), T("iron","raw","gravel"), T("rawiron","raw","gravel"), T("bauxite","raw","gravel"), T("rawbauxite","raw","gravel"), T("bitumen","raw","oil"), T("fuel","raw","oil"),
            T("uranium","nuclear","gravel"), T("yellowcake","nuclear","gravel"), T("uf6","nuclear","nuclear1"), T("nuclearfuel","nuclear","nuclear2"), T("nuclearfuelburned","nuclear","nuclear2"),
            T("fabric","consumer","covered"), T("alcohol","consumer","covered"), T("food","consumer","covered"), T("clothes","consumer","covered"), T("meat","consumer","cooler"), T("livestock","consumer","livestock"),
            T("ecomponents","industry","covered"), T("mcomponents","industry","covered"), T("plastics","industry","covered"), T("eletronics","industry","covered"), T("explosives","industry","covered"),
            T("water","water","water"), T("usagewater","water","water"), T("fertiliser_liquid","water","oil"), T("fertiliser","water","waste"),
            T("waste_gravel","waste","gravel"), T("waste_steel","waste","gravel"), T("waste_aluminium","waste","gravel"), T("waste_plastic","waste","open"), T("waste_bio","waste","waste"), T("waste_burnable","waste","waste"), T("waste_toxic","waste","waste"), T("waste_other","waste","waste"), T("waste_ash","waste","waste")
        };
        public static Template Find(string name) { return Templates.FirstOrDefault(t => t.Name.Equals((name ?? "").Trim(), StringComparison.OrdinalIgnoreCase)); }
        // Material families as resources.ini names them, with the engine number each stands for.
        public static readonly string[] Families = { "none", "gravel", "steel", "aluminium", "plastic", "bio", "food", "burnable", "toxic", "other", "ash" };
        public static string FamilyName(string value)
        {
            value = (value ?? "").Trim(); int n;
            if (Int32.TryParse(value, out n)) { if (n == -1) return "none"; if (n >= 10 && n <= 19) return Families[n - 9]; }
            return value;
        }
        // The first comma-separated token of a class line such as "oil, 1, 5, 5, 0".
        public static string Head(string value) { value = (value ?? "").Trim(); int comma = value.IndexOf(','); return comma < 0 ? value : value.Substring(0, comma).Trim(); }
    }

    public sealed class LocalDetailField
    {
        public string Id, Scope, Section, Key, Type, Label, LabelKey, Description, DescriptionKey;
        public string Group="";   // 0.4.29: item field of an extra [group:] ("" = the default group)
        public string Suffix="";  // 0.4.31: fixed tail of a key shown in a locked box behind a text field (".name")
        // 0.4.40: reference = game_research|game_buildings|game_texts - the value must name something the game knows;
        // reference_format id|file|requires|directive:$X, reference_own = group whose own entries count as known too.
        public string Reference="", ReferenceFormat="id", ReferenceDirective="", ReferenceOwn="";
        public bool AboveId;       // 0.4.35: position = above_id renders the field above the id row of the detail panel
        // heading / heading_key: a divider with a small bold title drawn above this
        // field in the detail panel, so long field lists fall into named blocks.
        public string Heading="", HeadingKey="";
        public string[] Choices = new string[0];
        // choice_prefix = 1: only the first comma-separated token has to be a choice;
        // what follows (factors, flags) is kept as written.
        public bool ChoicePrefix;
        // allow_other = 1: a choice list that also takes free text (a number, an
        // unlisted name). unique = 1: no two items may share the value.
        // auto_increment = 1: the add dialog proposes the highest value plus one.
        // dialog = 1: the field is part of the add dialog. choices_source = registry:
        // the choices are the source plugin's ids plus the base game's names.
        public bool AllowOther, Unique, AutoIncrement, InDialog;
        public string ChoicesSource="", Default="";
        // lines fields: picker = game_buildings opens the building picker; count_label
        // names the line counter ("Anzahl Gebaeude"); maximum_lines caps the rows.
        public string Picker="", CountLabel="", CountLabelKey="";
        public string PickerFormat="line";   // 0.4.42: research_lines picker - line | line_edit | line_anchor | anchor_edit | edit
        // 0.4.48: picker = files on a text field - a grouped list of the files under picker_folders (path specs
        // like [folder:], "|"-separated, first hit wins) matching picker_pattern; the value is the relative path.
        public string[] PickerFolders=new string[0]; public string PickerPattern="*";
        public int MaximumLines;
        // Which plugin-wide card a scope = global field sits on ("global" = the [global] card).
        public string Card="global";
        public int MaximumLength;
        // "<key>=<value>:<n>": a tighter length limit while another key of the same
        // item has the given value (the editor brush name under map = terrain).
        public string LengthRuleKey="", LengthRuleValue=""; public int LengthRuleLimit;
        public decimal Minimum, Maximum; public decimal Step=1;   // step: increment of the +/- buttons (0.4.39)
        public int Order;
        public string Normalize(string value)
        {
            value=(value??"").Trim();
            if(value.Length==0)return "";
            // lines: a key the plugin's INI repeats (target = ..., add = ...); one
            // value per line, blank lines dropped, stored joined by a newline.
            if(Type=="lines")
            {
                var kept=new List<string>();
                foreach(string raw in value.Replace("\r\n","\n").Split('\n'))
                {
                    string line=raw.Trim();if(line.Length==0)continue;
                    if(line.Length>1024||line.Any(c=>c=='\0')||line.StartsWith("[")||line.StartsWith(";")||line.StartsWith("#"))throw new FormatException(Msg.Key("err_ungueltiger_wert", Label));
                    kept.Add(line);
                }
                if(MaximumLines>0&&kept.Count>MaximumLines)throw new FormatException(Msg.Key("err_hoechstens_zeilen", Label, MaximumLines));
                return String.Join("\n",kept);
            }
            if(value.Length>1024||value.Any(c=>c=='\r'||c=='\n'||c=='\0'))throw new FormatException(Msg.Key("err_ungueltiger_wert", Label));
            if(MaximumLength>0&&value.Length>MaximumLength)throw new FormatException(Msg.Key("err_hoechstens_zeichen", Label, MaximumLength));
            if(Type=="text"){if(value.Any(c=>c==';'||c=='='||c=='['||c==']'))throw new FormatException(Msg.Key("err_ungueltiges_zeichen", Label));return value;}
            if(Type=="choice"&&AllowOther&&!Choices.Contains(value,StringComparer.OrdinalIgnoreCase)){if(value.Any(c=>c==';'||c=='='||c=='['||c==']'||c==','))throw new FormatException(Msg.Key("err_ungueltiges_zeichen", Label));return value;}
            if(Type=="boolean")
            {
                if(value=="1"||value.Equals("true",StringComparison.OrdinalIgnoreCase))return "1";
                if(value=="0"||value.Equals("false",StringComparison.OrdinalIgnoreCase))return "0";
                throw new FormatException(Msg.Key("err_nur_0_oder_1_2", Label));
            }
            if(Type=="choice")
            {
                string head=ChoicePrefix?ResourceCatalogData.Head(value):value;
                string mapped=Key.Equals("family",StringComparison.OrdinalIgnoreCase)?ResourceCatalogData.FamilyName(head):head;
                if(!Choices.Contains(mapped,StringComparer.OrdinalIgnoreCase))throw new FormatException(Msg.Key("err_ungueltige_auswahl_2", Label));
                string canonical=Choices.First(x=>x.Equals(mapped,StringComparison.OrdinalIgnoreCase));
                if(!ChoicePrefix||head.Length==value.Length)return canonical;
                return canonical+value.Substring(head.Length);
            }
            string[] parts=value.Split(',').Select(x=>x.Trim()).ToArray();
            int minimumParts=Type=="pair"?1:Type=="triple"?2:1, maximumParts=Type=="pair"?2:Type=="triple"?3:1;
            if(parts.Length<minimumParts||parts.Length>maximumParts)throw new FormatException(Msg.Key("err_ungueltige_anzahl_werte", Label));
            var normalized=new List<string>();
            foreach(string part in parts)
            {
                decimal number;
                if(!Decimal.TryParse(part,NumberStyles.Float,CultureInfo.InvariantCulture,out number)||number<Minimum||number>Maximum||Type=="integer"&&Decimal.Truncate(number)!=number)
                    throw new FormatException(Msg.Key("err_zahl_von_bis_erforderlich", Label, Minimum, Maximum));
                normalized.Add(number.ToString("0.############################",CultureInfo.InvariantCulture));
            }
            return String.Join(", ",normalized);
        }
    }

    // One column of a keyed_list line: `<id> = col1, col2, col3 ...`.
    public sealed class ListColumn
    {
        public string Id, Label, LabelKey, Description, DescriptionKey, Type, Default="";
        // heading / heading_key: a divider with a small bold title drawn above this
        // field in the detail panel, so long field lists fall into named blocks.
        public string Heading="", HeadingKey="";
        public string[] Choices=new string[0];
        public bool AllowOther, Required;
        public decimal Minimum, Maximum; public decimal Step=1;   // 0.4.39
        public string Reference="";   // 0.4.40: the value must be an id the game knows
        public int Order;
        public string Normalize(string value)
        {
            value=(value??"").Trim();
            if(value.Length==0){if(Required)throw new FormatException(Msg.Key("err_pflichtwert_fehlt", Label));return "";}
            if(value.Length>128||value.Any(c=>c==','||c=='\r'||c=='\n'||c=='\0'||c=='='||c==';'||c=='['||c==']'))throw new FormatException(Msg.Key("err_ungueltiger_wert", Label));
            if(Type=="choice")
            {
                string hit=Choices.FirstOrDefault(x=>x.Equals(value,StringComparison.OrdinalIgnoreCase));
                if(hit!=null)return hit;
                if(AllowOther)return value;
                throw new FormatException(Msg.Key("err_ungueltige_auswahl_2", Label));
            }
            if(Type=="decimal"||Type=="integer")
            {
                decimal number;
                if(!Decimal.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out number)||number<Minimum||number>Maximum||Type=="integer"&&Decimal.Truncate(number)!=number)
                    throw new FormatException(Msg.Key("err_zahl_von_bis_erforderlich", Label, Minimum, Maximum));
                return number.ToString("0.############################",CultureInfo.InvariantCulture);
            }
            return value;
        }
    }
    public static class ListTuple
    {
        public static string[] Parse(string raw){if(String.IsNullOrWhiteSpace(raw))return new string[0];return raw.Split(',').Select(x=>x.Trim()).ToArray();}
        public static string Render(IEnumerable<string> parts){var list=parts.Select(x=>(x??"").Trim()).ToList();while(list.Count>0&&list[list.Count-1].Length==0)list.RemoveAt(list.Count-1);return String.Join(", ",list);}
    }

    public sealed class LocalTab { public string Id, Label, LabelKey; public int Order; }
    public sealed class LocalCard { public string Id, Tab, Label="", LabelKey="", Description="", DescriptionKey="", Notice="", NoticeKey="", NoticeStyle="warning"; public int Order; }
    // Language: when set (de, en, ...) the link is shown only while that UI language is active.
    public sealed class LocalLink { public string Id, File, Label="", LabelKey="", Language=""; public int Order; }
    // [folder:<id>] / [file:<id>] (0.4.28): rows of a card that show a folder or a file of the
    // loader or the package instead of an INI value. `path` (folder) or `paths` (file; "tag=path"
    // candidates separated by |) use the prefixes vfs: (the loader's VFS root), build: (the loader
    // folder) and package: (the [links] root). A missing folder shows `missing` and, with
    // create = 1, a button that creates it. Tags: workshop, local.
    public sealed class LocalPathRow { public string Id, Card="global", Kind="folder", Label="", LabelKey="", Description="", DescriptionKey="", Missing="", MissingKey=""; public bool Create; public int Order; public readonly List<KeyValuePair<string,string>> Candidates=new List<KeyValuePair<string,string>>(); }
    // [group:<id>] with section_prefix (0.4.29): a further list of INI sections in a
    // keyed_sections editor, on its own tab, with its own texts ([list:<id>], [new:<id>]),
    // fields (detail group = <id>) and limit. Item ids of such a group are the complete
    // section names (prefix included), so they never collide with the default group.
    public sealed class ItemGroup
    {
        public string Id, Prefix, Tab, Label="", LabelKey="", Description="", DescriptionKey="", Notice="", NoticeKey="";
        public string ListLabel="", ListLabelKey="", ListNote="", ListNoteKey="", AddLabel="", AddLabelKey="", SelectHelp="", SelectHelpKey="", IdLabel="", IdLabelKey="", IdHelp="", IdHelpKey="", SaveWarning="", SaveWarningKey="", NameLabel="", NameLabelKey="", NewHint="", NewHintKey="";
        public string RemoveLabel="", RemoveLabelKey="";   // 0.4.35: button that removes only the entry
        public string IdReference="";   // 0.4.40: entry ids must be known to the game (game_research, game_texts)
        public string IdPicker="";      // 0.4.42: [list:<id>] id_picker = game_research|game_texts for the add dialog
        public string[] SummaryKeys=new string[0]; public int MaximumItems, Order;
    }
    // [research_block:<id>] (0.4.42): a read-only view of the entry's Vanilla research block
    // (media_soviet\research\research.ini), so edits of existing research are written against
    // the lines the game really has. Rendered among the fields by order.
    public sealed class LocalBlockRow
    {
        public string Id, Group="", Label="", LabelKey="", Description="", DescriptionKey="";
        public int Order;
    }
    // [picture:<id>] (0.4.35): a preview of a picture file that belongs to an entry ("{id}" in
    // `file` is the entry's id) plus "Insert picture...", which copies a chosen PNG there under
    // that name. `size` is the required width and height in pixels (0 = any).
    public sealed class LocalPictureRow { public string Id, Group="", Folder, File="{id}.png", Label="", LabelKey="", Description="", DescriptionKey=""; public int Size=128, Order; }

    public sealed class LocalEditorSpec
    {
        public string SchemaPath, SchemaHash, Id, Name, Version, Description, Icon, Plugin, ConfigName, ListSection, ItemSectionPrefix, LanguageDirectory;
        public string TabLabel, TabLabelKey, GroupLabel, GroupLabelKey, GroupDescription, GroupDescriptionKey, GroupNotice, GroupNoticeKey;
        // Tabs in order; the list card sits on GroupTab, the plugin-wide card on GlobalTab.
        public readonly List<LocalTab> Tabs=new List<LocalTab>();
        public string GroupTab="",GlobalTab="",DefaultTab="";public int GlobalOrder;
        public string IdReference="";   // 0.4.40: id_reference of the default group
        public string ActiveSection, ActiveKey;
        public string[] ActiveValues = new string[0];
        public int MaximumItems;
        public readonly List<LocalDetailField> Fields=new List<LocalDetailField>();
        // keyed_resources: resources.ini with template/display lines and [custom:] sections.
        // keyed_list: `<id> = col, col, ...` lines described by [column:] sections; new ids
        // come from another plugin's list ([source]).
        public string EditorType="keyed_resources";
        public readonly List<ListColumn> Columns=new List<ListColumn>();
        public string SourcePlugin="",SourceSection="",SourceReadySection="",SourceReadyKey="",SourceReadyValue="";
        public string ListLabel="",ListLabelKey="",ListNote="",ListNoteKey="",AddLabel="",AddLabelKey="",SelectHelp="",SelectHelpKey="",ItemIdLabel="",ItemIdLabelKey="",ItemIdHelp="",ItemIdHelpKey="";
        public bool ItemIdSuggestions=true;
        public string AddItemIdHelp="",AddItemIdHelpKey="";   // [list] add_id_help: help under the identifier in the add dialog only (0.4.22)
        public string ItemIdPicker="";   // "game_texts": a "Choose text..." button opens the game-caption picker (0.4.21)
        public string GlobalLabel="",GlobalLabelKey="",GlobalDescription="",GlobalDescriptionKey="";
        public bool HasGlobals{get{return Fields.Any(x=>x.Scope=="global");}}
        // Further cards for plugin-wide fields ([card:<id>], a field names its card) and
        // links to the package's guides ([link:<id>]) on the tab from [links].
        public readonly List<LocalCard> Cards=new List<LocalCard>();
        public readonly List<LocalLink> Links=new List<LocalLink>();
        public readonly List<LocalPathRow> PathRows=new List<LocalPathRow>();   // 0.4.28
        public readonly List<ItemGroup> ExtraGroups=new List<ItemGroup>();   // 0.4.29
        public TextPackSpec TextPack;   // 0.4.30: [textpack], null when the schema has none
        public readonly List<LocalPictureRow> Pictures=new List<LocalPictureRow>();   // 0.4.35
        public IEnumerable<LocalPictureRow> PicturesFor(ItemGroup group){string gid=group==null?"":group.Id;return Pictures.Where(x=>x.Group.Equals(gid,StringComparison.OrdinalIgnoreCase));}
        public readonly List<LocalBlockRow> Blocks=new List<LocalBlockRow>();   // 0.4.42
        public IEnumerable<LocalBlockRow> BlocksFor(ItemGroup group){string gid=group==null?"":group.Id;return Blocks.Where(x=>x.Group.Equals(gid,StringComparison.OrdinalIgnoreCase));}
        public string LocalizedBlockLabel(Language l,LocalBlockRow b){return Text(l,b.LabelKey,b.Label);}
        public string LocalizedBlockDescription(Language l,LocalBlockRow b){return Text(l,b.DescriptionKey,b.Description).Replace("\n","\n");}
        public string LocalizedPictureLabel(Language l,LocalPictureRow p){return Text(l,p.LabelKey,p.Label);}
        public string LocalizedPictureDescription(Language l,LocalPictureRow p){return Text(l,p.DescriptionKey,p.Description).Replace("\\n","\n");}
        public string RemoveLabel="",RemoveLabelKey="";   // [list] remove_label (0.4.35)
        public string LocalizedRemoveLabel(Language l,ItemGroup g){string s=g==null?Text(l,RemoveLabelKey,RemoveLabel):Text(l,g.RemoveLabelKey,g.RemoveLabel);return s.Length>0?s:l.T("remove_item_only");}
        public string LocalizedTextPackLabel(Language l){return TextPack==null?"":Text(l,TextPack.LabelKey,TextPack.Label);}
        public string LocalizedTextPackDescription(Language l){return TextPack==null?"":Text(l,TextPack.DescriptionKey,TextPack.Description).Replace("\\n","\n");}
        public string LocalizedTextPackMissing(Language l){return TextPack==null?"":Text(l,TextPack.MissingKey,TextPack.Missing).Replace("\\n","\n");}
        public ItemGroup GroupOfId(string id){if(id==null)return null;foreach(ItemGroup g in ExtraGroups)if(id.StartsWith(g.Prefix,StringComparison.OrdinalIgnoreCase))return g;return null;}
        public ItemGroup GroupById(string groupId){return String.IsNullOrEmpty(groupId)?null:ExtraGroups.FirstOrDefault(x=>x.Id.Equals(groupId,StringComparison.OrdinalIgnoreCase));}
        public IEnumerable<LocalDetailField> ItemFields(ItemGroup group){string gid=group==null?"":group.Id;return Fields.Where(x=>x.Scope=="item"&&x.Group.Equals(gid,StringComparison.OrdinalIgnoreCase));}
        public string DisplayId(string id){ItemGroup g=GroupOfId(id);return g==null?id:id.Substring(g.Prefix.Length);}
        public string IdPickerOf(ItemGroup g){return g==null?ItemIdPicker:g.IdPicker;}   // 0.4.42
        public string[] SummaryKeysOf(ItemGroup g){return g==null?SummaryKeys:g.SummaryKeys;}
        public int MaximumItemsOf(ItemGroup g){return g==null||g.MaximumItems<=0?MaximumItems:g.MaximumItems;}
        // Group-aware texts: the default group keeps the [group:]/[list]/[new] texts.
        public string LocalizedGroup(Language l,ItemGroup g){return g==null?LocalizedGroup(l):Text(l,g.LabelKey,g.Label);}
        public string LocalizedGroupDescription(Language l,ItemGroup g){return g==null?LocalizedGroupDescription(l):Text(l,g.DescriptionKey,g.Description).Replace("\\n","\n");}
        public string LocalizedGroupNotice(Language l,ItemGroup g){return g==null?LocalizedGroupNotice(l):Text(l,g.NoticeKey,g.Notice).Replace("\\n","\n");}
        public string LocalizedSaveWarning(Language l,ItemGroup g){return g==null?LocalizedSaveWarning(l):Text(l,g.SaveWarningKey,g.SaveWarning).Replace("\\n","\n");}
        public string LocalizedListLabel(Language l,ItemGroup g){return g==null?LocalizedListLabel(l):Text(l,g.ListLabelKey,g.ListLabel);}
        public string LocalizedListNote(Language l,ItemGroup g){return g==null?LocalizedListNote(l):Text(l,g.ListNoteKey,g.ListNote).Replace("\\n","\n");}
        public string LocalizedAddLabel(Language l,ItemGroup g){return g==null?LocalizedAddLabel(l):Text(l,g.AddLabelKey,g.AddLabel);}
        public string LocalizedSelectHelp(Language l,ItemGroup g){return g==null?LocalizedSelectHelp(l):Text(l,g.SelectHelpKey,g.SelectHelp).Replace("\\n","\n");}
        public string LocalizedItemIdLabel(Language l,ItemGroup g){return g==null?LocalizedItemIdLabel(l):Text(l,g.IdLabelKey,g.IdLabel);}
        public string LocalizedItemIdHelp(Language l,ItemGroup g){return g==null?LocalizedItemIdHelp(l):Text(l,g.IdHelpKey,g.IdHelp).Replace("\\n","\n");}
        public string LocalizedNameLabel(Language l,ItemGroup g){return g==null?LocalizedNameLabel(l):Text(l,g.NameLabelKey,g.NameLabel);}
        public string LocalizedNewHint(Language l,ItemGroup g){return g==null?LocalizedNewHint(l):Text(l,g.NewHintKey,g.NewHint).Replace("\\n","\n");}
        public string LocalizedPathLabel(Language language,LocalPathRow row){return Text(language,row.LabelKey,row.Label);}
        public string LocalizedPathDescription(Language language,LocalPathRow row){return Text(language,row.DescriptionKey,row.Description).Replace("\\n","\n");}
        public string LocalizedPathMissing(Language language,LocalPathRow row){return Text(language,row.MissingKey,row.Missing).Replace("\\n","\n");}
        // vfs:<rel>, build:<rel>, package:<rel> -> absolute path (0.4.28).
        public string ResolvePath(string spec,string build)
        {
            int colon=spec.IndexOf(':');if(colon<=0)throw new FormatException(Msg.Key("err_ungueltiger_pfadeintrag", spec));
            string prefix=spec.Substring(0,colon).Trim().ToLowerInvariant(),rel=spec.Substring(colon+1).Trim();
            string root;
            if(prefix=="vfs")root=VfsRoot(build);
            else if(prefix=="build")root=Path.GetFullPath(build);
            else if(prefix=="package")root=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(SchemaPath),LinksRoot));
            else throw new FormatException(Msg.Key("err_ungueltiger_pfadeintrag", spec));
            try{return SafeFiles.Child(root,rel);}catch(IOException){throw new FormatException(Msg.Key("err_ungueltiger_pfadeintrag", spec));}
        }
        // The loader's rule (ResolveVfsRoot in tesmioloader.cpp): "vfs" beside the loader folder
        // when it exists, else build\vfs. When neither exists the sibling is answered, because
        // that is where the loader looks first once it is created.
        public static string VfsRoot(string build)
        {
            string full=Path.GetFullPath(build).TrimEnd('\\','/');string parent=Path.GetDirectoryName(full);
            string sibling=parent==null?Path.Combine(full,"vfs"):Path.Combine(parent,"vfs");string inside=Path.Combine(full,"vfs");
            if(Directory.Exists(sibling))return sibling;if(Directory.Exists(inside))return inside;return sibling;
        }
        public string LinksTab="",LinksLabel="",LinksLabelKey="",LinksRoot=".";
        public string LocalizedLinksLabel(Language language){return Text(language,LinksLabelKey,LinksLabel);}
        public string LocalizedCardLabel(Language language,LocalCard card){return Text(language,card.LabelKey,card.Label);}
        public string LocalizedCardDescription(Language language,LocalCard card){return Text(language,card.DescriptionKey,card.Description).Replace("\\n","\n");}
        public string LocalizedCardNotice(Language language,LocalCard card){return Text(language,card.NoticeKey,card.Notice).Replace("\\n","\n");}
        public string LocalizedLinkLabel(Language language,LocalLink link){return Text(language,link.LabelKey,link.Label);}
        // Links for the active UI language: language-specific ones matching it, plus every link without a language.
        public List<LocalLink> LinksFor(Language language)
        {
            string code=(language.Code??"").Split('-')[0].ToLowerInvariant();
            var matching=Links.Where(x=>x.Language.Length==0||x.Language==code).ToList();
            // No guide in this language: fall back to every language-specific link.
            if(!matching.Any(x=>x.Language.Length>0)&&Links.Any(x=>x.Language.Length>0))matching.AddRange(Links.Where(x=>x.Language.Length>0));
            return matching;
        }
        // Every card in order: the [global] card first, then the [card:] sections.
        public List<LocalCard> AllCards()
        {
            var list=new List<LocalCard>{new LocalCard{Id="global",Tab=GlobalTab,Label=GlobalLabel,LabelKey=GlobalLabelKey,Description=GlobalDescription,DescriptionKey=GlobalDescriptionKey,Notice=GlobalNotice,NoticeKey=GlobalNoticeKey,Order=GlobalOrder}};
            list.AddRange(Cards);// [global] order (0.4.7) sorts the plugin-wide card among the [card:] entries; ties keep it first.
            return list.OrderBy(x=>x.Order).ThenBy(x=>x.Id=="global"?0:1).ToList();
        }
        public string LinkPath(LocalLink link)
        {
            string root=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(SchemaPath),LinksRoot));
            string path=SafeFiles.Child(root,link.File);string ext=Path.GetExtension(path).ToLowerInvariant();
            if(ext!=".md"&&ext!=".txt"&&ext!=".html"&&ext!=".pdf")throw new FormatException(Msg.Key("err_nur_md_txt_html", link.File));
            return path;
        }
        public bool IsList{get{return EditorType=="keyed_list";}}
        // keyed_sections: one INI section per item (deposits.ini); every section
        // except the reserved ones is an item, its keys are the item's fields.
        public bool IsSections{get{return EditorType=="keyed_sections";}}
        public bool HidesOriginals{get{return IsList||IsSections;}}
        public string[] ReservedSections=new string[0], SummaryKeys=new string[0];
        public string SectionPrefix="";
        public string NameLabel="",NameLabelKey="",TokenLabel="",TokenLabelKey="",TokenKey="",TokenTemplate="",NewHint="",NewHintKey="",GlobalNotice="",GlobalNoticeKey="",SaveWarning="",SaveWarningKey="",Notice="",NoticeKey="",NoticeStyle="info",Info="",InfoKey="";
        public string LocalizedSaveWarning(Language language){return Text(language,SaveWarningKey,SaveWarning).Replace("\\n","\n");}
        readonly Ini schema;
        static bool Token(string value){return !String.IsNullOrWhiteSpace(value)&&value.Length<64&&Regex.IsMatch(value,"^[A-Za-z0-9][A-Za-z0-9._-]*$");}
        static int Order(Ini ini,string section){int n;return Int32.TryParse(ini.Get(section,"order","0"),out n)?n:0;}
        LocalEditorSpec(Ini parsed){schema=parsed;}
        public static LocalEditorSpec Load(string path)
        {
            path=Path.GetFullPath(path);SafeFiles.NoLinks(path);byte[] bytes=SafeFiles.Read(path,1024*1024);var ini=new Ini(SafeFiles.Decode(bytes));
            string editorType=ini.Get("launcher","editor_type","");
            string tabSection=ini.Sections.FirstOrDefault(x=>x.StartsWith("tab:",StringComparison.OrdinalIgnoreCase))??"tab:resources",groupSection=ini.Sections.FirstOrDefault(x=>x.StartsWith("group:",StringComparison.OrdinalIgnoreCase)&&ini.Get(x,"section_prefix","").Length==0)??ini.Sections.FirstOrDefault(x=>x.StartsWith("group:",StringComparison.OrdinalIgnoreCase))??"group:resources";
            if((editorType!="keyed_resources"&&editorType!="keyed_list"&&editorType!="keyed_sections")||ini.Get("launcher","layout_version")!="1")throw new FormatException(Msg.Key("err_nicht_unterstuetzter_lokaler_editor"));
            var s=new LocalEditorSpec(ini){SchemaPath=path,SchemaHash=SafeFiles.Hash(bytes),Id=ini.Required("launcher","id"),Name=ini.Required("launcher","name"),Version=ini.Get("launcher","version","1"),Description=ini.Get("launcher","description",""),Icon=ini.Get("launcher","icon","builtin:gear"),LanguageDirectory=ini.Get("launcher","language_directory",""),
                Plugin=ini.Required("editor","plugin"),ConfigName=ini.Required("editor","config"),ListSection=ini.Get("editor","list_section","list"),ItemSectionPrefix=ini.Get("editor","item_section_prefix","custom:"),
                TabLabel=ini.Get(tabSection,"label","Resources"),TabLabelKey=ini.Get(tabSection,"label_key",""),GroupLabel=ini.Get(groupSection,"label","Resources"),GroupLabelKey=ini.Get(groupSection,"label_key",""),GroupDescription=ini.Get(groupSection,"description",""),GroupDescriptionKey=ini.Get(groupSection,"description_key",""),GroupNotice=ini.Get(groupSection,"notice",""),GroupNoticeKey=ini.Get(groupSection,"notice_key",""),
                ActiveSection=ini.Get("activity","section",""),ActiveKey=ini.Get("activity","key","")};
            if(!Token(s.Id)||!Token(s.Plugin)||Path.GetFileName(s.ConfigName)!=s.ConfigName||!s.ConfigName.EndsWith(".ini",StringComparison.OrdinalIgnoreCase)||!Token(Path.GetFileNameWithoutExtension(s.ConfigName))||!Token(s.ListSection)||s.ItemSectionPrefix.Length==0||s.ItemSectionPrefix.Any(Char.IsControl))throw new FormatException(Msg.Key("err_ungueltige_lokale_editor_kennung"));
            if(!Int32.TryParse(ini.Get("editor","maximum_items","256"),NumberStyles.None,CultureInfo.InvariantCulture,out s.MaximumItems)||s.MaximumItems<1||s.MaximumItems>1024)throw new FormatException(Msg.Key("err_ungueltige_maximum_items"));
            s.ActiveValues=ini.Get("activity","values","").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray();
            s.EditorType=editorType;
            s.ListLabel=ini.Get("list","label","");s.ListLabelKey=ini.Get("list","label_key","");s.ListNote=ini.Get("list","note","");s.ListNoteKey=ini.Get("list","note_key","");s.AddLabel=ini.Get("list","add_label","");s.AddLabelKey=ini.Get("list","add_label_key","");
            s.SelectHelp=ini.Get("list","select_help","");s.SelectHelpKey=ini.Get("list","select_help_key","");s.ItemIdLabel=ini.Get("list","id_label","");s.ItemIdLabelKey=ini.Get("list","id_label_key","");s.ItemIdHelp=ini.Get("list","id_help","");s.ItemIdHelpKey=ini.Get("list","id_help_key","");
            // [list] id_suggestions = 0: the identifier is not a resource name, so the add dialog offers a plain typed field (0.4.20).
            s.ItemIdSuggestions=ini.Get("list","id_suggestions","1")!="0";
            s.AddItemIdHelp=ini.Get("list","add_id_help","");s.AddItemIdHelpKey=ini.Get("list","add_id_help_key","");
            s.ItemIdPicker=ini.Get("list","id_picker","").Trim();if(s.ItemIdPicker.Length>0&&s.ItemIdPicker!="game_texts"&&s.ItemIdPicker!="game_research")throw new FormatException(Msg.Key("err_unbekannter_id_picker", s.ItemIdPicker));
            s.GlobalLabel=ini.Get("global","label","");s.GlobalLabelKey=ini.Get("global","label_key","");s.GlobalDescription=ini.Get("global","description","");s.GlobalDescriptionKey=ini.Get("global","description_key","");
            foreach(string section in ini.Sections.Where(x=>x.StartsWith("tab:",StringComparison.OrdinalIgnoreCase)))
            {string id=section.Substring(4);if(!Token(id)||s.Tabs.Any(x=>x.Id.Equals(id,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_ungueltiger_reiter", section));s.Tabs.Add(new LocalTab{Id=id,Label=ini.Get(section,"label",id),LabelKey=ini.Get(section,"label_key",""),Order=Order(ini,section)});}
            s.Tabs.Sort((a,b)=>a.Order!=b.Order?a.Order.CompareTo(b.Order):String.CompareOrdinal(a.Id,b.Id));
            if(s.Tabs.Count==0)s.Tabs.Add(new LocalTab{Id="resources",Label="Resources"});
            s.IdReference=ini.Get(groupSection,"id_reference","").Trim();if(s.IdReference.Length>0&&!ReferenceSets.Known(s.IdReference))throw new FormatException(Msg.Key("err_ungueltiger_verweis", groupSection));
            s.GroupTab=ini.Get(groupSection,"tab",s.Tabs[0].Id);s.GlobalTab=ini.Get("global","tab",s.Tabs[0].Id);s.GlobalOrder=Order(ini,"global");s.DefaultTab=ini.Get("launcher","default_tab",s.Tabs[0].Id);
            foreach(string tab in new[]{s.GroupTab,s.GlobalTab,s.DefaultTab})if(!s.Tabs.Any(x=>x.Id.Equals(tab,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_unbekannter_reiter", tab));
            foreach(string section in ini.Sections.Where(x=>x.StartsWith("card:",StringComparison.OrdinalIgnoreCase)))
            {
                var c=new LocalCard{Id=section.Substring(5),Tab=ini.Get(section,"tab",s.GlobalTab),Label=ini.Get(section,"label",section.Substring(5)),LabelKey=ini.Get(section,"label_key",""),Description=ini.Get(section,"description",""),DescriptionKey=ini.Get(section,"description_key",""),Notice=ini.Get(section,"notice",""),NoticeKey=ini.Get(section,"notice_key",""),NoticeStyle=ini.Get(section,"notice_style","warning"),Order=Order(ini,section)};
                if(!Token(c.Id)||c.Id.Equals("global",StringComparison.OrdinalIgnoreCase)||s.Cards.Any(x=>x.Id.Equals(c.Id,StringComparison.OrdinalIgnoreCase))||!s.Tabs.Any(x=>x.Id.Equals(c.Tab,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_ungueltige_karte", section));
                s.Cards.Add(c);
            }
            s.Cards.Sort((a,b)=>a.Order!=b.Order?a.Order.CompareTo(b.Order):String.CompareOrdinal(a.Id,b.Id));
            s.LinksTab=ini.Get("links","tab",s.GlobalTab);s.LinksLabel=ini.Get("links","label","");s.LinksLabelKey=ini.Get("links","label_key","");s.LinksRoot=ini.Get("links","root",".");
            if(!s.Tabs.Any(x=>x.Id.Equals(s.LinksTab,StringComparison.OrdinalIgnoreCase))||s.LinksRoot.Any(c=>c==':'||c=='\0')||s.LinksRoot.Split('\\','/').Any(part=>part!=".."&&part!="."&&part.Length>0&&!Token(part)))throw new FormatException(Msg.Key("err_ungueltiger_links_abschnitt"));
            foreach(string section in ini.Sections.Where(x=>x.StartsWith("link:",StringComparison.OrdinalIgnoreCase)))
            {
                var l=new LocalLink{Id=section.Substring(5),File=ini.Required(section,"file"),Label=ini.Get(section,"label",section.Substring(5)),LabelKey=ini.Get(section,"label_key",""),Language=ini.Get(section,"language","").Trim().ToLowerInvariant(),Order=Order(ini,section)};
                if(!Token(l.Id)||l.File.Length==0||l.File.Any(c=>c==':'||c=='\0')||l.File.Contains("..")||l.Language.Length>0&&!Language.ValidCode(l.Language))throw new FormatException(Msg.Key("err_ungueltige_verknuepfung", section));
                s.Links.Add(l);
            }
            s.Links.Sort((a,b)=>a.Order!=b.Order?a.Order.CompareTo(b.Order):String.CompareOrdinal(a.Id,b.Id));
            // [folder:<id>] / [file:<id>] (0.4.28): path rows of the plugin-wide cards.
            foreach(string section in ini.Sections.Where(x=>x.StartsWith("folder:",StringComparison.OrdinalIgnoreCase)||x.StartsWith("file:",StringComparison.OrdinalIgnoreCase)))
            {
                bool folder=section.StartsWith("folder:",StringComparison.OrdinalIgnoreCase);
                var r=new LocalPathRow{Id=section.Substring(folder?7:5),Kind=folder?"folder":"file",Card=ini.Get(section,"card","global"),Label=ini.Get(section,"label",section),LabelKey=ini.Get(section,"label_key",""),Description=ini.Get(section,"description",""),DescriptionKey=ini.Get(section,"description_key",""),Missing=ini.Get(section,"missing",""),MissingKey=ini.Get(section,"missing_key",""),Create=ini.Get(section,"create","0")=="1",Order=Order(ini,section)};
                foreach(string part in (folder?ini.Required(section,"path"):ini.Required(section,"paths")).Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0))
                {
                    string tag="",spec=part;int eq=part.IndexOf('=');
                    if(!folder&&eq>0){tag=part.Substring(0,eq).Trim().ToLowerInvariant();spec=part.Substring(eq+1).Trim();}
                    if(tag.Length>0&&tag!="workshop"&&tag!="local")throw new FormatException(Msg.Key("err_ungueltiger_pfadeintrag", part));
                    s.ResolvePath(spec,Path.GetTempPath());   // syntax check only
                    r.Candidates.Add(new KeyValuePair<string,string>(tag,spec));
                }
                if(!Token(r.Id)||r.Candidates.Count==0||(folder&&r.Candidates.Count!=1)||!(r.Card.Equals("global",StringComparison.OrdinalIgnoreCase)||s.Cards.Any(x=>x.Id.Equals(r.Card,StringComparison.OrdinalIgnoreCase)))||s.PathRows.Any(x=>x.Id.Equals(r.Id,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_ungueltiger_pfadeintrag", section));
                s.PathRows.Add(r);
            }
            s.PathRows.Sort((a,b)=>a.Order!=b.Order?a.Order.CompareTo(b.Order):String.CompareOrdinal(a.Id,b.Id));
            s.GlobalNotice=ini.Get("global","notice","");s.GlobalNoticeKey=ini.Get("global","notice_key","");
            // [launcher] notice: package-wide note for the "Notes" card of the first tab, like presentation schemas have it.
            s.Notice=ini.Get("launcher","notice","");s.NoticeKey=ini.Get("launcher","notice_key","");s.NoticeStyle=ini.Get("launcher","notice_style","info");s.Info=ini.Get("launcher","info","");s.InfoKey=ini.Get("launcher","info_key","");s.SaveWarning=ini.Get("list","save_warning","");s.SaveWarningKey=ini.Get("list","save_warning_key","");
            if(s.IsSections)
            {
                s.ReservedSections=ini.Get("editor","reserved_sections","").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray();
                // section_prefix = modify: -> only [modify:<id>] sections are items; every
                // other section and every free line of the file stays untouched.
                s.SectionPrefix=ini.Get("editor","section_prefix","").Trim();
                if(s.SectionPrefix.Any(c=>c=='['||c==']'||c=='='||c==';'||Char.IsWhiteSpace(c)))throw new FormatException(Msg.Key("err_ungueltige_lokale_editor_kennung"));
                s.SummaryKeys=ini.Get("list","summary","").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray();
                s.RemoveLabel=ini.Get("list","remove_label","");s.RemoveLabelKey=ini.Get("list","remove_label_key","");
                // [group:<id>] with section_prefix (0.4.29): a further list of sections on its own
                // tab; texts from [list:<id>] and [new:<id>], fields carry group = <id>.
                foreach(string section in ini.Sections.Where(x=>x.StartsWith("group:",StringComparison.OrdinalIgnoreCase)&&!x.Equals(groupSection,StringComparison.OrdinalIgnoreCase)&&ini.Get(x,"section_prefix","").Length>0))
                {
                    string gid=section.Substring(6),ls="list:"+gid,ns="new:"+gid;
                    var g=new ItemGroup{Id=gid,Prefix=ini.Get(section,"section_prefix","").Trim(),Tab=ini.Get(section,"tab",s.GroupTab),Label=ini.Get(section,"label",gid),LabelKey=ini.Get(section,"label_key",""),Description=ini.Get(section,"description",""),DescriptionKey=ini.Get(section,"description_key",""),Notice=ini.Get(section,"notice",""),NoticeKey=ini.Get(section,"notice_key",""),Order=Order(ini,section),
                        ListLabel=ini.Get(ls,"label",""),ListLabelKey=ini.Get(ls,"label_key",""),ListNote=ini.Get(ls,"note",""),ListNoteKey=ini.Get(ls,"note_key",""),AddLabel=ini.Get(ls,"add_label",""),AddLabelKey=ini.Get(ls,"add_label_key",""),SelectHelp=ini.Get(ls,"select_help",""),SelectHelpKey=ini.Get(ls,"select_help_key",""),IdLabel=ini.Get(ls,"id_label",""),IdLabelKey=ini.Get(ls,"id_label_key",""),IdHelp=ini.Get(ls,"id_help",""),IdHelpKey=ini.Get(ls,"id_help_key",""),SaveWarning=ini.Get(ls,"save_warning",""),SaveWarningKey=ini.Get(ls,"save_warning_key",""),
                        RemoveLabel=ini.Get(ls,"remove_label",""),RemoveLabelKey=ini.Get(ls,"remove_label_key",""),NameLabel=ini.Get(ns,"name_label",""),NameLabelKey=ini.Get(ns,"name_label_key",""),NewHint=ini.Get(ns,"hint",""),NewHintKey=ini.Get(ns,"hint_key",""),SummaryKeys=ini.Get(ls,"summary","").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray()};
                    int limit;if(!Int32.TryParse(ini.Get(ls,"maximum_items","0"),NumberStyles.None,CultureInfo.InvariantCulture,out limit))limit=0;g.MaximumItems=limit;
                    g.IdReference=ini.Get(section,"id_reference","").Trim();if(g.IdReference.Length>0&&!ReferenceSets.Known(g.IdReference))throw new FormatException(Msg.Key("err_ungueltiger_verweis", section));
                    g.IdPicker=ini.Get(ls,"id_picker","").Trim();if(g.IdPicker.Length>0&&g.IdPicker!="game_texts"&&g.IdPicker!="game_research")throw new FormatException(Msg.Key("err_ungueltige_listengruppe", section));
                    if(!Token(g.Id)||g.Prefix.Any(c=>c=='['||c==']'||c=='='||c==';'||Char.IsWhiteSpace(c))||g.Prefix.Equals(s.SectionPrefix,StringComparison.OrdinalIgnoreCase)||s.ExtraGroups.Any(x=>x.Id.Equals(g.Id,StringComparison.OrdinalIgnoreCase)||x.Prefix.Equals(g.Prefix,StringComparison.OrdinalIgnoreCase))||!s.Tabs.Any(x=>x.Id.Equals(g.Tab,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_ungueltige_listengruppe", section));
                    s.ExtraGroups.Add(g);
                }
                s.ExtraGroups.Sort((a,b)=>a.Order!=b.Order?a.Order.CompareTo(b.Order):String.CompareOrdinal(a.Id,b.Id));
                s.NameLabel=ini.Get("new","name_label","");s.NameLabelKey=ini.Get("new","name_label_key","");s.TokenLabel=ini.Get("new","token_label","");s.TokenLabelKey=ini.Get("new","token_label_key","");
                s.TokenKey=ini.Get("new","token_key","");s.TokenTemplate=ini.Get("new","token_template","");s.NewHint=ini.Get("new","hint","");s.NewHintKey=ini.Get("new","hint_key","");
                if(s.TokenKey.Length>0&&!Token(s.TokenKey))throw new FormatException(Msg.Key("err_ungueltiger_token_key"));
            }
            if(s.IsList||s.IsSections)
            {
                s.SourcePlugin=ini.Get("source","plugin","");s.SourceSection=ini.Get("source","section","list");
                s.SourceReadySection=ini.Get("source","ready_section","");s.SourceReadyKey=ini.Get("source","ready_key","");s.SourceReadyValue=ini.Get("source","ready_value","");
                if(s.SourcePlugin.Length>0&&(!Token(s.SourcePlugin)||!Token(s.SourceSection)))throw new FormatException(Msg.Key("err_ungueltige_listenquelle"));
                foreach(string section in ini.Sections.Where(x=>x.StartsWith("column:",StringComparison.OrdinalIgnoreCase)))
                {
                    string type=ini.Get(section,"type","text");decimal minimum=0,maximum=1000000;
                    if(!new[]{"text","integer","decimal","choice"}.Contains(type))throw new FormatException(Msg.Key("err_ungueltige_spalte", section));
                    if(type=="integer"||type=="decimal"){minimum=Decimal.Parse(ini.Get(section,"minimum","-1000000"),CultureInfo.InvariantCulture);maximum=Decimal.Parse(ini.Get(section,"maximum","1000000"),CultureInfo.InvariantCulture);if(minimum>maximum)throw new FormatException(Msg.Key("err_ungueltiger_zahlenbereich", section));}
                    var c=new ListColumn{Id=section.Substring(7),Type=type,Label=ini.Get(section,"label",section.Substring(7)),LabelKey=ini.Get(section,"label_key",""),Description=ini.Get(section,"description",""),DescriptionKey=ini.Get(section,"description_key",""),Heading=ini.Get(section,"heading",""),HeadingKey=ini.Get(section,"heading_key",""),Choices=ini.Get(section,"choices","").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray(),AllowOther=ini.Get(section,"allow_other","0")=="1",Required=ini.Get(section,"required","0")=="1",Default=ini.Get(section,"default",""),Minimum=minimum,Maximum=maximum,Order=Order(ini,section)};
                    if(type=="integer"||type=="decimal"){decimal step;if(!Decimal.TryParse(ini.Get(section,"step",type=="integer"?"1":"0.1"),NumberStyles.Float,CultureInfo.InvariantCulture,out step)||step<=0)throw new FormatException(Msg.Key("err_ungueltiger_zahlenbereich", section));c.Step=step;}
                    c.Reference=ini.Get(section,"reference","").Trim();if(c.Reference.Length>0&&!ReferenceSets.Known(c.Reference))throw new FormatException(Msg.Key("err_ungueltiger_verweis", section));
                    if(!Token(c.Id)||type=="choice"&&c.Choices.Length==0||s.Columns.Any(x=>x.Id.Equals(c.Id,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_unvollstaendige_spalte", section));
                    s.Columns.Add(c);
                }
                s.Columns.Sort((a,b)=>a.Order!=b.Order?a.Order.CompareTo(b.Order):String.CompareOrdinal(a.Id,b.Id));
                if(s.IsList&&s.Columns.Count==0)throw new FormatException(Msg.Key("err_ein_keyed_list_editor"));
            }
            foreach(string section in ini.Sections.Where(x=>x.StartsWith("detail:",StringComparison.OrdinalIgnoreCase)))
            {
                string type=ini.Get(section,"type","text"),scope=ini.Required(section,"scope");decimal minimum=0,maximum=1000000;
                if(!new[]{"text","integer","decimal","choice","pair","triple","boolean","lines"}.Contains(type)||scope!="custom"&&scope!="keyed"&&scope!="global"&&scope!="item")throw new FormatException(Msg.Key("err_ungueltiges_detailfeld", section));
                if(type=="lines"&&scope!="item")throw new FormatException(Msg.Key("err_ungueltiges_detailfeld", section));
                if(scope=="item"&&!s.IsSections)throw new FormatException(Msg.Key("err_scope_item_gibt_es", section));
                if(type=="integer"||type=="decimal"||type=="pair"||type=="triple")
                {minimum=Decimal.Parse(ini.Get(section,"minimum","-1000000"),CultureInfo.InvariantCulture);maximum=Decimal.Parse(ini.Get(section,"maximum","1000000"),CultureInfo.InvariantCulture);if(minimum>maximum)throw new FormatException(Msg.Key("err_ungueltiger_zahlenbereich", section));}
                var f=new LocalDetailField{Id=section.Substring(7),Scope=scope,Section=ini.Get(section,"section",""),Key=ini.Required(section,"key"),Type=type,Label=ini.Get(section,"label",section.Substring(7)),LabelKey=ini.Get(section,"label_key",""),Description=ini.Get(section,"description",""),DescriptionKey=ini.Get(section,"description_key",""),Heading=ini.Get(section,"heading",""),HeadingKey=ini.Get(section,"heading_key",""),Choices=ini.Get(section,"choices","").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray(),ChoicePrefix=ini.Get(section,"choice_prefix","0")=="1",Minimum=minimum,Maximum=maximum,Order=Order(ini,section),AllowOther=ini.Get(section,"allow_other","0")=="1",Unique=ini.Get(section,"unique","0")=="1",AutoIncrement=ini.Get(section,"auto_increment","0")=="1",InDialog=ini.Get(section,"dialog","0")=="1",ChoicesSource=ini.Get(section,"choices_source",""),Default=ini.Get(section,"default",""),Card=ini.Get(section,"card","global")};
                if(!Int32.TryParse(ini.Get(section,"maximum_length","0"),NumberStyles.None,CultureInfo.InvariantCulture,out f.MaximumLength))throw new FormatException(Msg.Key("err_ungueltige_maximum_length", section));
                if(type=="integer"||type=="decimal"){decimal step;if(!Decimal.TryParse(ini.Get(section,"step",type=="integer"?"1":"0.1"),NumberStyles.Float,CultureInfo.InvariantCulture,out step)||step<=0)throw new FormatException(Msg.Key("err_ungueltiger_zahlenbereich", section));f.Step=step;}
                f.Picker=ini.Get(section,"picker","");f.CountLabel=ini.Get(section,"count_label","");f.CountLabelKey=ini.Get(section,"count_label_key","");f.PickerFormat=ini.Get(section,"picker_format","line").Trim();
                f.PickerFolders=ini.Get(section,"picker_folders","").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray();f.PickerPattern=ini.Get(section,"picker_pattern","*").Trim();
                foreach(string spec in f.PickerFolders)s.ResolvePath(spec,Path.GetTempPath());   // syntax check only
                string position=ini.Get(section,"position","").Trim();if(position.Length>0&&(position!="above_id"||scope!="item"))throw new FormatException(Msg.Key("err_ungueltiges_detailfeld", section));f.AboveId=position=="above_id";
                f.Suffix=ini.Get(section,"suffix","").Trim();if(f.Suffix.Length>0&&(type!="text"||scope!="item"))throw new FormatException(Msg.Key("err_ungueltiges_detailfeld", section));
                f.Group=ini.Get(section,"group","").Trim();if(f.Group.Length>0&&(scope!="item"||!s.ExtraGroups.Any(x=>x.Id.Equals(f.Group,StringComparison.OrdinalIgnoreCase))))throw new FormatException(Msg.Key("err_ungueltiges_detailfeld", section));
                // reference (0.4.40): the value must name something the game knows; checked in Validate.
                f.Reference=ini.Get(section,"reference","").Trim();f.ReferenceOwn=ini.Get(section,"reference_own","").Trim();string refFormat=ini.Get(section,"reference_format","id").Trim();if(refFormat.StartsWith("directive:",StringComparison.OrdinalIgnoreCase)){f.ReferenceDirective=refFormat.Substring(10).Trim();refFormat="directive";}f.ReferenceFormat=refFormat;
                if(f.Reference.Length>0&&(!(ReferenceSets.Known(f.Reference)||f.Reference=="files")||scope!="item"||!(type=="text"||type=="lines"||type=="choice")||!new[]{"id","file","requires","directive","exists","dds_dxt1","dds_dxt5"}.Contains(refFormat)||refFormat=="directive"&&f.ReferenceDirective.Length==0||f.ReferenceOwn.Length>0&&s.GroupById(f.ReferenceOwn)==null||f.Reference=="files"&&(f.Picker!="files"||!new[]{"id","exists","dds_dxt1","dds_dxt5"}.Contains(refFormat))))throw new FormatException(Msg.Key("err_ungueltiger_verweis", section));
                if(!Int32.TryParse(ini.Get(section,"maximum_lines","0"),NumberStyles.None,CultureInfo.InvariantCulture,out f.MaximumLines)||f.Picker.Length>0&&(f.Picker=="files"?(type!="text"||f.PickerFolders.Length==0):(f.Picker!="game_buildings"&&f.Picker!="game_research"&&f.Picker!="research_lines"||type!="lines"))||!new[]{"line","line_edit","line_anchor","anchor_edit","edit"}.Contains(f.PickerFormat))throw new FormatException(Msg.Key("err_ungueltiges_detailfeld", section));
                string rule=ini.Get(section,"length_rule","");
                if(rule.Length>0){int eq=rule.IndexOf('='),colon=rule.LastIndexOf(':');if(eq<=0||colon<=eq||!Int32.TryParse(rule.Substring(colon+1),NumberStyles.None,CultureInfo.InvariantCulture,out f.LengthRuleLimit))throw new FormatException(Msg.Key("err_ungueltige_length_rule", section));f.LengthRuleKey=rule.Substring(0,eq).Trim();f.LengthRuleValue=rule.Substring(eq+1,colon-eq-1).Trim();}
                if(!Token(f.Id)||!Token(f.Key)||(scope=="keyed"||scope=="global")&&!Token(f.Section)||type=="choice"&&f.Choices.Length==0&&f.ChoicesSource.Length==0||s.Fields.Any(x=>x.Id.Equals(f.Id,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_unvollstaendiges_detailfeld", section));
                if(f.ChoicesSource.Length>0&&f.ChoicesSource!="registry")throw new FormatException(Msg.Key("err_unbekannte_choices_source", section));
                s.Fields.Add(f);
            }
            // [picture:<id>] (0.4.35): picture rows of the section details.
            foreach(string section in ini.Sections.Where(x=>x.StartsWith("picture:",StringComparison.OrdinalIgnoreCase)))
            {
                int size;if(!Int32.TryParse(ini.Get(section,"size","128"),NumberStyles.None,CultureInfo.InvariantCulture,out size))size=-1;
                var p=new LocalPictureRow{Id=section.Substring(8),Group=ini.Get(section,"group","").Trim(),Folder=ini.Required(section,"folder"),File=ini.Get(section,"file","{id}.png").Trim(),Label=ini.Get(section,"label",section),LabelKey=ini.Get(section,"label_key",""),Description=ini.Get(section,"description",""),DescriptionKey=ini.Get(section,"description_key",""),Size=size,Order=Order(ini,section)};
                s.ResolvePath(p.Folder,Path.GetTempPath());   // syntax check only
                if(!Token(p.Id)||!s.IsSections||size<0||!p.File.Contains("{id}")||p.File.IndexOfAny(new[]{'\\','/',':'})>=0||(p.Group.Length>0&&s.GroupById(p.Group)==null)||s.Pictures.Any(x=>x.Id.Equals(p.Id,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_ungueltiges_bild", section));
                s.Pictures.Add(p);
            }
            s.Pictures.Sort((a,b)=>a.Order!=b.Order?a.Order.CompareTo(b.Order):String.CompareOrdinal(a.Id,b.Id));
            // [research_block:<id>] (0.4.42): read-only Vanilla block rows of the section details.
            foreach(string section in ini.Sections.Where(x=>x.StartsWith("research_block:",StringComparison.OrdinalIgnoreCase)))
            {
                var b=new LocalBlockRow{Id=section.Substring(15),Group=ini.Get(section,"group","").Trim(),Label=ini.Get(section,"label",section),LabelKey=ini.Get(section,"label_key",""),Description=ini.Get(section,"description",""),DescriptionKey=ini.Get(section,"description_key",""),Order=Order(ini,section)};
                if(!Token(b.Id)||!s.IsSections||(b.Group.Length>0&&s.GroupById(b.Group)==null)||s.Blocks.Any(x=>x.Id.Equals(b.Id,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_ungueltiger_forschungsblock", section));
                s.Blocks.Add(b);
            }
            s.Blocks.Sort((a,b)=>a.Order!=b.Order?a.Order.CompareTo(b.Order):String.CompareOrdinal(a.Id,b.Id));
            // [textpack] (0.4.30): a Localization text pack edited on its own tab.
            if(ini.Sections.Any(x=>x.Equals("textpack",StringComparison.OrdinalIgnoreCase)))
            {
                var tp=new TextPackSpec{Tab=ini.Get("textpack","tab",s.GlobalTab),Folder=ini.Required("textpack","folder"),Namespace=ini.Get("textpack","namespace",""),KeysFrom=ini.Get("textpack","keys_from","").Trim(),Label=ini.Get("textpack","label","Localization"),LabelKey=ini.Get("textpack","label_key",""),Description=ini.Get("textpack","description",""),DescriptionKey=ini.Get("textpack","description_key",""),Missing=ini.Get("textpack","missing",""),MissingKey=ini.Get("textpack","missing_key","")};
                string seed=ini.Get("textpack","seed","");
                if(seed.Length>0){int bar=seed.IndexOf('|');if(!seed.StartsWith("dependency:",StringComparison.OrdinalIgnoreCase)||bar<12)throw new FormatException(Msg.Key("err_ungueltiges_textpaket", seed));tp.SeedDependency=seed.Substring(11,bar-11).Trim();tp.SeedPath=seed.Substring(bar+1).Trim();if(tp.SeedPath.Length==0||tp.SeedPath.Contains("..")||tp.SeedPath.Contains(":"))throw new FormatException(Msg.Key("err_ungueltiges_textpaket", seed));}
                s.ResolvePath(tp.Folder,Path.GetTempPath());   // syntax check only
                if(!s.Tabs.Any(x=>x.Id.Equals(tp.Tab,StringComparison.OrdinalIgnoreCase))||(tp.KeysFrom.Length>0&&s.GroupById(tp.KeysFrom)==null)||(tp.Namespace.Length>0&&!Token(tp.Namespace)))throw new FormatException(Msg.Key("err_ungueltiges_textpaket", tp.Tab));
                s.TextPack=tp;
            }
            s.Fields.Sort((a,b)=>a.Order!=b.Order?a.Order.CompareTo(b.Order):String.CompareOrdinal(a.Id,b.Id));return s;
        }
        Ini Translation(string code)
        {
            if(LanguageDirectory.Length==0||!Language.ValidCode(code))return null;
            try{string root=Path.GetDirectoryName(SchemaPath),file=SafeFiles.Child(root,LanguageDirectory+"\\"+code+".ini");return File.Exists(file)?new Ini(SafeFiles.Text(file)):null;}catch{return null;}
        }
        string Text(Language language,string key,string fallback)
        {
            if(key.Length==0)return fallback;Ini selected=Translation(language.Code),english=Translation("en");return (selected==null?null:selected.Get("strings",key))??(english==null?null:english.Get("strings",key))??fallback;
        }
        public string LocalizedName(Language language){return Text(language,schema.Get("launcher","name_key",""),Name);}
        public string LocalizedDescription(Language language){return Text(language,schema.Get("launcher","description_key",""),Description).Replace("\\n","\n");}
        public string LocalizedTab(Language language){return Text(language,TabLabelKey,TabLabel);}
        public string LocalizedTabLabel(Language language,LocalTab tab){return Text(language,tab.LabelKey,tab.Label);}
        // The plugin-wide field the [activity] key points at, if the schema has one.
        public LocalDetailField ActivityField{get{return ActiveSection.Length==0?null:Fields.FirstOrDefault(x=>x.Scope=="global"&&x.Section.Equals(ActiveSection,StringComparison.OrdinalIgnoreCase)&&x.Key.Equals(ActiveKey,StringComparison.OrdinalIgnoreCase));}}
        public string LocalizedGroup(Language language){return Text(language,GroupLabelKey,GroupLabel);}
        public string LocalizedGroupDescription(Language language){return Text(language,GroupDescriptionKey,GroupDescription).Replace("\\n","\n");}
        // Blue information box on the list card, between the description and the yellow save warning.
        public string LocalizedGroupNotice(Language language){return Text(language,GroupNoticeKey,GroupNotice).Replace("\\n","\n");}
        public string FieldLabel(Language language,LocalDetailField field){return Text(language,field.LabelKey,field.Label);}
        // A literal \n in a field description becomes a line break, as it already does for list columns.
        public string FieldDescription(Language language,LocalDetailField field){return Text(language,field.DescriptionKey,field.Description).Replace("\\n","\n");}
        public string FieldHeading(Language language,LocalDetailField field){return Text(language,field.HeadingKey,field.Heading);}
        public string FieldCountLabel(Language language,LocalDetailField field){return Text(language,field.CountLabelKey,field.CountLabel);}
        public string ColumnLabel(Language language,ListColumn column){return Text(language,column.LabelKey,column.Label);}
        public string ColumnHeading(Language language,ListColumn column){return Text(language,column.HeadingKey,column.Heading);}
        public string ColumnDescription(Language language,ListColumn column){return Text(language,column.DescriptionKey,column.Description).Replace("\\n","\n");}
        public string LocalizedListLabel(Language language){return Text(language,ListLabelKey,ListLabel);}
        public string LocalizedListNote(Language language){return Text(language,ListNoteKey,ListNote).Replace("\\n","\n");}
        public string LocalizedAddLabel(Language language){return Text(language,AddLabelKey,AddLabel);}
        public string LocalizedSelectHelp(Language language){return Text(language,SelectHelpKey,SelectHelp).Replace("\\n","\n");}
        public string LocalizedItemIdLabel(Language language){return Text(language,ItemIdLabelKey,ItemIdLabel);}
        public string LocalizedItemIdHelp(Language language){return Text(language,ItemIdHelpKey,ItemIdHelp).Replace("\\n","\n");}
        // The add dialog's own help text; without one the detail-pane help is shown there too.
        public string LocalizedAddItemIdHelp(Language language){string own=Text(language,AddItemIdHelpKey,AddItemIdHelp).Replace("\\n","\n");return own.Length>0?own:LocalizedItemIdHelp(language);}
        public string LocalizedGlobalLabel(Language language){return Text(language,GlobalLabelKey,GlobalLabel);}
        public string LocalizedGlobalNotice(Language language){return Text(language,GlobalNoticeKey,GlobalNotice).Replace("\\n","\n");}
        public string LocalizedNotice(Language language){return Text(language,NoticeKey,Notice).Replace("\\n","\n");}
        public string LocalizedInfo(Language language){return Text(language,InfoKey,Info).Replace("\\n","\n");}
        public string LocalizedNameLabel(Language language){return Text(language,NameLabelKey,NameLabel);}
        public string LocalizedTokenLabel(Language language){return Text(language,TokenLabelKey,TokenLabel);}
        public string LocalizedNewHint(Language language){return Text(language,NewHintKey,NewHint).Replace("\\n","\n");}
        // The token the add dialog proposes for a new section: {NAME} upper case, {name} as typed.
        public string ProposedToken(string name){return TokenTemplate.Replace("{NAME}",(name??"").Trim().ToUpperInvariant()).Replace("{name}",(name??"").Trim());}
        public bool IsReserved(string section){return ReservedSections.Any(x=>x.Equals(section,StringComparison.OrdinalIgnoreCase));}
        public string LocalizedGlobalDescription(Language language){return Text(language,GlobalDescriptionKey,GlobalDescription).Replace("\\n","\n");}
        // A keyed_list line, normalised column by column. Gaps in the middle take the
        // column default so the line the plugin reads never has an empty field.
        public string NormalizeTuple(string raw)
        {
            string[] parts=ListTuple.Parse(raw);var result=new List<string>();
            for(int i=0;i<Columns.Count;i++){string value=i<parts.Length?parts[i]:"";result.Add(Columns[i].Normalize(value));}
            if(parts.Length>Columns.Count)throw new FormatException(Msg.Key("err_zu_viele_werte_in", parts.Length, Columns.Count));
            for(int i=0;i<result.Count;i++)if(result[i].Length==0&&result.Skip(i+1).Any(x=>x.Length>0))result[i]=Columns[i].Normalize(Columns[i].Default);
            return ListTuple.Render(result);
        }
        // The line as the editor shows it: every column filled, defaults where the INI is silent.
        public string[] TupleColumns(string raw)
        {
            string[] parts=ListTuple.Parse(raw);var result=new string[Columns.Count];
            for(int i=0;i<Columns.Count;i++)result[i]=i<parts.Length&&parts[i].Length>0?parts[i]:Columns[i].Default;return result;
        }
        public string Summary(string raw){return String.Join(" · ",TupleColumns(raw).Take(3).Where(x=>x.Length>0));}
        public void AssertUnchanged(){if(SafeFiles.HashFile(SchemaPath)!=SchemaHash)throw new IOException(Msg.Key("err_editor_schema_wurde_inzwischen", SchemaPath));}
    }

    public sealed class ResourceListValue
    {
        public string Slot="",Template="",Display="";
        public static ResourceListValue Parse(string raw)
        {
            string[] parts=(raw??"").Split(',').Select(x=>x.Trim()).ToArray();var value=new ResourceListValue();int index=0,slot;
            if(parts.Length>1&&Int32.TryParse(parts[0],NumberStyles.None,CultureInfo.InvariantCulture,out slot)){value.Slot=parts[0];index=1;}
            if(index<parts.Length)value.Template=parts[index++];if(index<parts.Length)value.Display=String.Join(", ",parts.Skip(index));return value;
        }
        public string Render(){var parts=new List<string>();if(Slot.Length>0)parts.Add(Slot);parts.Add(Template);if(Display.Length>0)parts.Add(Display);return String.Join(", ",parts);}
    }

    public sealed class LooseIni
    {
        sealed class Line { public string Raw,Section,Key,Value; public bool Header; }
        readonly List<Line> lines=new List<Line>();public readonly string Newline;
        public LooseIni(string text){Newline=text.Contains("\r\n")?"\r\n":"\n";Parse(text);}
        void Parse(string text)
        {
            lines.Clear();string section="";foreach(string raw in text.Replace("\r\n","\n").Split('\n'))
            {string t=raw.Trim();var line=new Line{Raw=raw,Section=section};if(t.StartsWith("[")&&t.EndsWith("]")&&t.Length>2){section=t.Substring(1,t.Length-2).Trim();line.Section=section;line.Header=true;}else if(t.Length>0&&!t.StartsWith(";")&&!t.StartsWith("#")){int eq=t.IndexOf('=');if(eq>0){line.Key=t.Substring(0,eq).Trim();line.Value=t.Substring(eq+1).Trim();line.Section=section;}}lines.Add(line);}
        }
        public LooseIni Clone(){return new LooseIni(Render());}
        List<Line> Matches(string section,string key){return lines.Where(x=>x.Key!=null&&x.Section.Equals(section,StringComparison.OrdinalIgnoreCase)&&x.Key.Equals(key,StringComparison.OrdinalIgnoreCase)).ToList();}
        public string Get(string section,string key){var found=Matches(section,key);if(found.Count>1)throw new FormatException(Msg.Key("err_mehrdeutiger_doppelter_schluessel", section, key));return found.Count==0?null:found[0].Value;}
        public List<KeyValuePair<string,string>> Entries(string section)
        {return lines.Where(x=>x.Key!=null&&x.Section.Equals(section,StringComparison.OrdinalIgnoreCase)).Select(x=>new KeyValuePair<string,string>(x.Key,x.Value)).ToList();}
        public void Set(string section,string key,string value)
        {
            if(String.IsNullOrWhiteSpace(section)||String.IsNullOrWhiteSpace(key)||String.IsNullOrWhiteSpace(value)||value.Any(c=>c=='\r'||c=='\n'||c=='\0'))throw new FormatException(Msg.Key("err_ungueltiger_ini_eintrag", section, key));
            var found=Matches(section,key);if(found.Count>1)throw new FormatException(Msg.Key("err_mehrdeutiger_doppelter_schluessel", section, key));
            if(found.Count==1){Line line=found[0];int eq=line.Raw.IndexOf('=');line.Raw=(eq<0?key+" =":line.Raw.Substring(0,eq+1))+" "+value;line.Value=value;return;}
            int header=lines.FindIndex(x=>x.Header&&x.Section.Equals(section,StringComparison.OrdinalIgnoreCase));
            if(header<0){if(lines.Count>0&&lines[lines.Count-1].Raw.Length>0)lines.Add(new Line{Raw="",Section=""});lines.Add(new Line{Raw="["+section+"]",Section=section,Header=true});lines.Add(new Line{Raw=key+" = "+value,Section=section,Key=key,Value=value});return;}
            int insert=header+1;while(insert<lines.Count&&!lines[insert].Header)insert++;lines.Insert(insert,new Line{Raw=key+" = "+value,Section=section,Key=key,Value=value});
        }
        public void Remove(string section,string key)
        {foreach(Line line in Matches(section,key).ToList())lines.Remove(line);}
        // A key the file repeats (target = ..., add = ...): every value in file order.
        public List<string> GetAll(string section,string key){return Matches(section,key).Select(x=>x.Value).ToList();}
        // Rewrites all occurrences of a repeated key as one consecutive block where the
        // first occurrence was, or at the end of the section; an empty list removes them.
        public void SetAll(string section,string key,IEnumerable<string> values)
        {
            var list=values.Select(v=>(v??"").Trim()).Where(v=>v.Length>0).ToList();
            if(String.IsNullOrWhiteSpace(section)||String.IsNullOrWhiteSpace(key)||list.Any(v=>v.Any(c=>c=='\r'||c=='\n'||c=='\0')))throw new FormatException(Msg.Key("err_ungueltiger_ini_eintrag", section, key));
            var found=Matches(section,key);int at;
            if(found.Count>0){at=lines.IndexOf(found[0]);foreach(Line line in found)lines.Remove(line);}
            else
            {
                EnsureSection(section);int header=lines.FindIndex(x=>x.Header&&x.Section.Equals(section,StringComparison.OrdinalIgnoreCase));
                at=header+1;while(at<lines.Count&&!lines[at].Header)at++;while(at>header+1&&lines[at-1].Raw.Trim().Length==0)at--;
            }
            for(int i=0;i<list.Count;i++)lines.Insert(at+i,new Line{Raw=key+" = "+list[i],Section=section,Key=key,Value=list[i]});
        }
        // Section headers in file order (a repeated header counts once).
        public List<string> SectionNames(){var result=new List<string>();foreach(Line line in lines)if(line.Header&&!result.Contains(line.Section,StringComparer.OrdinalIgnoreCase))result.Add(line.Section);return result;}
        public bool HasSection(string section){return lines.Any(x=>x.Header&&x.Section.Equals(section,StringComparison.OrdinalIgnoreCase));}
        public void EnsureSection(string section)
        {if(HasSection(section))return;if(lines.Count>0&&lines[lines.Count-1].Raw.Length>0)lines.Add(new Line{Raw="",Section=""});lines.Add(new Line{Raw="["+section+"]",Section=section,Header=true});}
        // Drops the header, every line up to the next header, and the comment block
        // directly above the header (the section's own description).
        static bool IsComment(Line line){string t=line.Raw.Trim();return t.StartsWith(";")||t.StartsWith("#");}
        public void RemoveSection(string section)
        {
            for(int i=0;i<lines.Count;)
            {
                if(lines[i].Header&&lines[i].Section.Equals(section,StringComparison.OrdinalIgnoreCase)){int end=i+1;while(end<lines.Count&&!lines[end].Header)end++;int start=i;while(start>0&&IsComment(lines[start-1]))start--;lines.RemoveRange(start,end-start);i=start;}
                else i++;
            }
        }
        public string Render(){return String.Join(Newline,lines.Select(x=>x.Raw));}
    }

    public sealed class ResourceItemOverride
    {
        // Suppressed: an original line the user hid; it stays in the upstream file and
        // is left out of the effective one. Only meaningful for keyed_list editors.
        public string Id,ListValue="";public bool Owned,Suppressed;public readonly Dictionary<string,string> Fields=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    }
    // Orders "field.<id>.<n>" keys by id, then numerically by n (so .10 follows .9).
    sealed class LineKeyOrder : IComparer<string>
    {
        public int Compare(string a,string b)
        {
            string ia=Head(a),ib=Head(b);int byName=String.Compare(ia,ib,StringComparison.OrdinalIgnoreCase);if(byName!=0)return byName;
            return Index(a).CompareTo(Index(b));
        }
        static string Head(string key){int dot=key.LastIndexOf('.');int n;return dot>0&&Int32.TryParse(key.Substring(dot+1),NumberStyles.None,CultureInfo.InvariantCulture,out n)?key.Substring(0,dot):key;}
        static int Index(string key){int dot=key.LastIndexOf('.');int n;return dot>0&&Int32.TryParse(key.Substring(dot+1),NumberStyles.None,CultureInfo.InvariantCulture,out n)?n:-1;}
    }
    public sealed class ResourceOverrideStore
    {
        public readonly Dictionary<string,ResourceItemOverride> Items=new Dictionary<string,ResourceItemOverride>(StringComparer.OrdinalIgnoreCase);
        // Plugin-wide values (scope = global details), keyed by field id.
        public readonly Dictionary<string,string> Globals=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public ResourceOverrideStore Clone(){return Parse(Render());}
        public static ResourceOverrideStore Parse(string text)
        {
            var store=new ResourceOverrideStore();if(String.IsNullOrWhiteSpace(text))return store;var ini=new Ini(text);if(ini.Get("state","format")!="1")throw new FormatException(Msg.Key("err_unbekanntes_resources_override_format"));
            foreach(string section in ini.Sections.Where(x=>x.StartsWith("item:",StringComparison.OrdinalIgnoreCase)))
            {string id=section.Substring(5);if(!CollectionRules.SafeItemOrPrefixed(id)||store.Items.ContainsKey(id))throw new FormatException(Msg.Key("err_ungueltige_oder_doppelte_ressourcenkennung", id));var item=new ResourceItemOverride{Id=id,Owned=ini.Get(section,"owned","0")=="1",Suppressed=ini.Get(section,"suppressed","0")=="1",ListValue=ini.Get(section,"list","")};foreach(var pair in ini.Values.Where(x=>x.Key.StartsWith(section.ToLowerInvariant()+"/field.",StringComparison.OrdinalIgnoreCase)).OrderBy(x=>x.Key,new LineKeyOrder())){string name=pair.Key.Substring(pair.Key.IndexOf("/field.",StringComparison.OrdinalIgnoreCase)+7);int dot=name.LastIndexOf('.');int index;if(dot>0&&Int32.TryParse(name.Substring(dot+1),NumberStyles.None,CultureInfo.InvariantCulture,out index)){name=name.Substring(0,dot);string current;item.Fields[name]=item.Fields.TryGetValue(name,out current)&&current.Length>0?current+"\n"+pair.Value:pair.Value;}else item.Fields[name]=pair.Value;}store.Items.Add(id,item);}
            foreach(var pair in ini.Values.Where(x=>x.Key.StartsWith("global/field.",StringComparison.OrdinalIgnoreCase)))store.Globals[pair.Key.Substring(13)]=pair.Value;
            return store;
        }
        public string Render()
        {
            var b=new StringBuilder("; Personal overrides. The upstream INI remains the baseline.\r\n\r\n[state]\r\nformat = 1\r\n");
            if(Globals.Count>0){b.Append("\r\n[global]\r\n");foreach(var g in Globals.OrderBy(x=>x.Key,StringComparer.OrdinalIgnoreCase))b.Append("field."+g.Key+" = "+g.Value+"\r\n");}
            foreach(var item in Items.Values.OrderBy(x=>x.Id,StringComparer.OrdinalIgnoreCase))
            {b.Append("\r\n[item:"+item.Id+"]\r\n");if(item.Owned)b.Append("owned = 1\r\n");if(item.Suppressed)b.Append("suppressed = 1\r\n");if(item.ListValue.Length>0)b.Append("list = "+item.ListValue+"\r\n");foreach(var field in item.Fields.OrderBy(x=>x.Key,StringComparer.OrdinalIgnoreCase)){if(field.Value.IndexOf('\n')<0)b.Append("field."+field.Key+" = "+field.Value+"\r\n");else{string[] all=field.Value.Split('\n');for(int i=0;i<all.Length;i++)b.Append("field."+field.Key+"."+i+" = "+all[i]+"\r\n");}}}return b.ToString();
        }
    }

    public sealed class LocalResourceItem
    {
        public string Id,ListValue,Template,Display,Subtitle="";public bool Owned;
        public override string ToString(){return (Display.Length==0?Id:Display)+"  ["+Id+"]";}
    }

    public sealed class LocalResourceSession
    {
        public readonly LocalEditorSpec Spec;public readonly string Build,LocalIni,LocalDll,UserIni,UpstreamFile,Receipt;
        public readonly Dictionary<string,string> Before=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);public readonly List<string> Notes=new List<string>();
        public readonly List<string> DisappearedExternal=new List<string>();
        public ResourceOverrideStore Overrides;string upstreamText,initialOverrides;readonly string localAtOpen;bool refreshUpstream;
        readonly Dictionary<string,byte[]> dependentWrites=new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);
        // Package-backed editor (a keyed schema shipped in a Workshop package): the
        // baseline is the package's own INI, so a Steam update is the new original at
        // once; the DLL may live in the package (bridge, SML) or in plugins\ (local
        // copy), and the loader entry belongs to the package's Session, not to us.
        public readonly bool ExternalLoader;
        public LocalResourceSession(LocalEditorSpec spec,string build):this(spec,build,null){}
        public LocalResourceSession(LocalEditorSpec spec,string build,Package package)
        {
            Spec=spec;Build=Path.GetFullPath(build);LocalIni=SafeFiles.Child(Build,"plugins\\"+Spec.ConfigName);LocalDll=SafeFiles.Child(Build,"plugins\\"+Spec.Plugin+".dll");UserIni=SafeFiles.Child(Build,"user_config\\"+Spec.Plugin+".editor.ini");UpstreamFile=SafeFiles.Child(Build,"user_config\\.autoload\\"+Spec.Plugin+".upstream.ini");Receipt=SafeFiles.Child(Build,"user_config\\.autoload\\"+Spec.Plugin+".editor.receipt.ini");LoaderIni=SafeFiles.Child(Build,"tesmioloader.ini");
            ExternalLoader=package!=null;
            if(package==null&&(!File.Exists(LocalDll)||!File.Exists(LocalIni)))throw new IOException(Msg.Key("err_benoetigte_lokale_dateien_fehlen", Spec.Plugin, Spec.ConfigName));
            if(package!=null&&(package.DefaultsBytes==null||!package.Target.Equals(Spec.Plugin,StringComparison.OrdinalIgnoreCase)||!package.ConfigName.Equals(Spec.ConfigName,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_editor_schema_passt_nicht"));
            // DLL and loader entry belong to the package's Session when the editor is
            // package-backed; only the files this editor writes are watched then.
            foreach(string path in package==null?new[]{LocalIni,LocalDll,UserIni,UpstreamFile,Receipt,LoaderIni}:new[]{LocalIni,UserIni,UpstreamFile,Receipt})Before[path]=SafeFiles.HashFile(path);
            LoaderEnabled=ReadLoaderSwitch();
            localAtOpen=File.Exists(LocalIni)?SafeFiles.Text(LocalIni):"";string localHash=Before[LocalIni];string recorded="";if(File.Exists(Receipt))try{recorded=new Ini(SafeFiles.Text(Receipt)).Get("state","effective_hash","");}catch{}
            Overrides=File.Exists(UserIni)?ResourceOverrideStore.Parse(SafeFiles.Text(UserIni)):new ResourceOverrideStore();
            if(package!=null)
            {
                upstreamText=SafeFiles.Decode(package.DefaultsBytes);
                refreshUpstream=!File.Exists(UpstreamFile)||SafeFiles.Text(UpstreamFile)!=upstreamText;
                Notes.Add(Msg.Key("res_note_package_base", package.Version, "user_config\\"+Spec.Plugin+".editor.ini", "plugins\\"+Spec.ConfigName));
            }
            else if(File.Exists(UpstreamFile)&&recorded==localHash)upstreamText=SafeFiles.Text(UpstreamFile);else
            {
                string previous=File.Exists(UpstreamFile)?SafeFiles.Text(UpstreamFile):"";var incoming=new LooseIni(localAtOpen);
                // If somebody edited the effective file by hand, entries previously created by
                // Settings are still present there. They remain personal and must not suddenly
                // become an undeletable external baseline.
                foreach(var owned in Overrides.Items.Values.Where(x=>x.Owned))
                {
                    string found=incoming.Get(Spec.ListSection,owned.Id);
                    if(found!=null&&found.Equals(owned.ListValue,StringComparison.OrdinalIgnoreCase))incoming.Remove(Spec.ListSection,owned.Id);
                }
                upstreamText=incoming.Render();refreshUpstream=true;
                if(previous.Length>0)
                {
                    var priorIds=new HashSet<string>(new LooseIni(previous).Entries(Spec.ListSection).Select(x=>x.Key),StringComparer.OrdinalIgnoreCase);
                    var nowIds=new HashSet<string>(new LooseIni(upstreamText).Entries(Spec.ListSection).Select(x=>x.Key),StringComparer.OrdinalIgnoreCase);
                    DisappearedExternal.AddRange(priorIds.Where(x=>!nowIds.Contains(x)).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase));
                    foreach(string missing in DisappearedExternal){ResourceItemOverride change;if(Overrides.Items.TryGetValue(missing,out change)&&!change.Owned)Overrides.Items.Remove(missing);}
                }
                Notes.Add(Msg.Key(File.Exists(UpstreamFile)?"res_note_external_update":"res_note_taken_as_base", Spec.ConfigName));
            }
            initialOverrides=Overrides.Render();Validate();Notes.Add(Msg.Key("res_note_originals_fixed"));
        }
        Dictionary<string,string> UpstreamItems()
        {var result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);if(Spec.IsSections){foreach(string section in new LooseIni(upstreamText).SectionNames()){if(Spec.IsReserved(section))continue;string id=section;ItemGroup extra=Spec.GroupOfId(section);if(extra!=null){if(!CollectionRules.SafeItem(section.Substring(extra.Prefix.Length).Trim()))throw new FormatException(Msg.Key("err_ungueltiger_abschnittsname", section));result.Add(section,"");continue;}if(Spec.SectionPrefix.Length>0){if(!section.StartsWith(Spec.SectionPrefix,StringComparison.OrdinalIgnoreCase))continue;id=section.Substring(Spec.SectionPrefix.Length).Trim();}if(!CollectionRules.SafeItem(id))throw new FormatException(Msg.Key("err_ungueltiger_abschnittsname", section));result.Add(id,"");}return result;}foreach(var pair in new LooseIni(upstreamText).Entries(Spec.ListSection)){if(!CollectionRules.SafeItem(pair.Key)||result.ContainsKey(pair.Key))throw new FormatException(Msg.Key("err_ungueltige_oder_doppelte_ressource_2", Spec.ListSection, pair.Key));result.Add(pair.Key,pair.Value);}return result;}
        ResourceItemOverride Ensure(string id,bool owned){ResourceItemOverride item;if(!Overrides.Items.TryGetValue(id,out item)){item=new ResourceItemOverride{Id=id,Owned=owned};Overrides.Items.Add(id,item);}return item;}
        // The INI section behind an item: the id itself, or prefix + id when the
        // schema names a section prefix ("modify:" -> [modify:<id>]).
        string SectionOf(string id){return Spec.GroupOfId(id)!=null?id:Spec.SectionPrefix+id;}
        // Whether a field belongs to the group of the item (0.4.29).
        bool FieldApplies(LocalDetailField field,string id){return ReferenceEquals(Spec.GroupOfId(id),Spec.GroupById(field.Group));}
        LocalResourceItem Describe(string id,string raw,bool owned)
        {
            if(Spec.IsSections){ItemGroup group=Spec.GroupOfId(id);var parts=new List<string>();foreach(string key in Spec.SummaryKeysOf(group)){LocalDetailField field=Spec.ItemFields(group).FirstOrDefault(x=>x.Key.Equals(key,StringComparison.OrdinalIgnoreCase));string shown=field==null?"":Value(id,field);if(field!=null&&field.Type=="lines"&&shown.Length>0){string[] all=shown.Split('\n');shown=all[0]+(all.Length>1?" (+"+(all.Length-1)+")":"");}if(shown.Length>0)parts.Add(shown);}return new LocalResourceItem{Id=id,ListValue="",Template="",Display=Spec.DisplayId(id),Subtitle=String.Join(" · ",parts),Owned=owned};}
            if(Spec.IsList){string[] columns=Spec.TupleColumns(raw);return new LocalResourceItem{Id=id,ListValue=raw,Template=columns.Length>0?columns[0]:"",Display=id,Subtitle=Spec.Summary(raw),Owned=owned};}
            var value=ResourceListValue.Parse(raw);return new LocalResourceItem{Id=id,ListValue=raw,Template=value.Template,Display=value.Display,Subtitle=id,Owned=owned};
        }
        public List<LocalResourceItem> Items()
        {
            var source=UpstreamItems();var result=new List<LocalResourceItem>();
            foreach(var pair in source){ResourceItemOverride change;bool has=Overrides.Items.TryGetValue(pair.Key,out change);if(has&&change.Suppressed)continue;string raw=has&&change.ListValue.Length>0?change.ListValue:pair.Value;result.Add(Describe(pair.Key,raw,false));}
            foreach(var change in Overrides.Items.Values.Where(x=>x.Owned&&!source.ContainsKey(x.Id)).OrderBy(x=>x.Id,StringComparer.OrdinalIgnoreCase))result.Add(Describe(change.Id,change.ListValue,true));
            return result;
        }
        // The items of one group: null = the default group (0.4.29).
        public List<LocalResourceItem> Items(ItemGroup group){return Items().Where(x=>ReferenceEquals(Spec.GroupOfId(x.Id),group)).ToList();}
        // Original lines the user hid (keyed_list only); "Zuruecksetzen" brings them back.
        public List<string> SuppressedIds(){var source=UpstreamItems();return Overrides.Items.Values.Where(x=>x.Suppressed&&source.ContainsKey(x.Id)).Select(x=>x.Id).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();}
        // ---- keyed_list lines ----
        public void SetListRaw(string id,string raw)
        {
            if(!Spec.IsList)throw new InvalidOperationException(Msg.Key("err_nur_fuer_listeneditoren"));raw=Spec.NormalizeTuple(raw);var source=UpstreamItems();bool owned=!source.ContainsKey(id);var item=Ensure(id,owned);
            if(!owned&&raw.Equals(Spec.NormalizeTuple(source[id]),StringComparison.Ordinal)){item.ListValue="";if(item.Fields.Count==0&&!item.Suppressed)Overrides.Items.Remove(id);}else item.ListValue=raw;
        }
        public void AddRaw(string id,string raw)
        {
            if(!Spec.IsList)throw new InvalidOperationException(Msg.Key("err_nur_fuer_listeneditoren"));var previous=Overrides.Clone();
            try
            {
                id=(id??"").Trim();if(!CollectionRules.SafeItem(id))throw new FormatException(Msg.Key("err_ungueltige_kennung", id));
                if(Items().Any(x=>x.Id.Equals(id,StringComparison.OrdinalIgnoreCase))||SuppressedIds().Any(x=>x.Equals(id,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_eintrag_ist_bereits_vorhanden", id));
                if(Items().Count>=Spec.MaximumItems)throw new FormatException(Msg.Key("err_hoechstens_eintraege_sind_erlaubt", Spec.MaximumItems));
                var item=Ensure(id,true);item.ListValue=Spec.NormalizeTuple(raw);if(item.ListValue.Length==0)throw new FormatException(Msg.Key("err_die_zeile_braucht_mindestens"));Validate();
            }
            catch{Overrides=previous;throw;}
        }
        // ---- keyed_sections items ----
        // The add dialog's proposal for an auto_increment field: the highest value in
        // use plus one, never below the field's minimum.
        public string NextValue(LocalDetailField field)
        {
            decimal best=field.Minimum-1;foreach(var item in Items()){decimal n;if(Decimal.TryParse(Value(item.Id,field),NumberStyles.Float,CultureInfo.InvariantCulture,out n)&&n>best)best=n;}
            foreach(string id in SuppressedIds()){decimal n;if(Decimal.TryParse(BaselineValue(id,field),NumberStyles.Float,CultureInfo.InvariantCulture,out n)&&n>best)best=n;}
            decimal next=best+1;if(next<field.Minimum)next=field.Minimum;return next.ToString(CultureInfo.InvariantCulture);
        }
        public void AddSection(string name,IDictionary<string,string> values){AddSection(name,values,null);}
        // group = null adds to the default group; an extra group prefixes the name with its
        // section prefix and takes only its own fields (0.4.29).
        public void AddSection(string name,IDictionary<string,string> values,ItemGroup group)
        {
            if(!Spec.IsSections)throw new InvalidOperationException(Msg.Key("err_nur_fuer_abschnittseditoren"));var previous=Overrides.Clone();
            try
            {
                string typed=(name??"").Trim();name=CollectionRules.IdFromName(typed);   // "Technical Service Storage" -> technical_service_storage (0.4.25)
                if(!CollectionRules.SafeItem(name)||Spec.IsReserved(name))throw new FormatException(Msg.Key("err_ungueltiger_name", typed));
                string id=group==null?name:group.Prefix+name;
                if(Items().Any(x=>x.Id.Equals(id,StringComparison.OrdinalIgnoreCase))||SuppressedIds().Any(x=>x.Equals(id,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_eintrag_ist_bereits_vorhanden", name));
                if(Items(group).Count>=Spec.MaximumItemsOf(group))throw new FormatException(Msg.Key("err_hoechstens_eintraege_sind_erlaubt", Spec.MaximumItemsOf(group)));
                var item=Ensure(id,true);
                foreach(var pair in values){LocalDetailField field=Spec.ItemFields(group).FirstOrDefault(x=>x.Id.Equals(pair.Key,StringComparison.OrdinalIgnoreCase));if(field==null)throw new FormatException(Msg.Key("err_unbekanntes_feld", pair.Key));string value=field.Normalize(pair.Value);if(value.Length>0)item.Fields[field.Id]=value;}
                if(item.Fields.Count==0)throw new FormatException(Msg.Key("err_der_eintrag_braucht_mindestens"));Validate();
            }
            catch{Overrides=previous;throw;}
        }
        // Hide an original line from the effective file; it stays in the upstream copy.
        public void Suppress(string id)
        {
            if(!Spec.HidesOriginals)throw new InvalidOperationException(Msg.Key("err_nur_fuer_listen_und"));var source=UpstreamItems();if(!source.ContainsKey(id))throw new FormatException(Msg.Key("err_kein_original_eintrag", id));
            var item=Ensure(id,false);item.Suppressed=true;item.ListValue="";item.Fields.Clear();Validate();
        }
        public void Unsuppress(string id){ResourceItemOverride item;if(Overrides.Items.TryGetValue(id,out item)&&item.Suppressed){item.Suppressed=false;if(item.ListValue.Length==0&&item.Fields.Count==0)Overrides.Items.Remove(id);}Validate();}
        // ---- plugin-wide (scope = global) values ----
        public string GlobalBaseline(LocalDetailField field){var doc=new LooseIni(upstreamText);return doc.Get(field.Section,field.Key)??"";}
        public string GlobalValue(LocalDetailField field){string value;return Overrides.Globals.TryGetValue(field.Id,out value)?value:GlobalBaseline(field);}
        public void SetGlobal(LocalDetailField field,string value)
        {
            value=field.Normalize(value);string baseline=GlobalBaseline(field);
            if(value.Length==0||value==baseline)Overrides.Globals.Remove(field.Id);else Overrides.Globals[field.Id]=value;
        }
        public string BaselineValue(string id,LocalDetailField field)
        {var source=UpstreamItems();if(!source.ContainsKey(id))return "";var doc=new LooseIni(upstreamText);if(field.Scope=="item")return field.Type=="lines"?String.Join("\n",doc.GetAll(SectionOf(id),field.Key)):(doc.Get(SectionOf(id),field.Key)??"");return field.Scope=="custom"?(doc.Get(Spec.ItemSectionPrefix+id,field.Key)??""):(doc.Get(field.Section,id)??"");}
        public string OriginalListValue(string id){string value;return UpstreamItems().TryGetValue(id,out value)?value:"";}
        public string Value(string id,LocalDetailField field)
        {ResourceItemOverride item;string value;if(Overrides.Items.TryGetValue(id,out item)&&item.Fields.TryGetValue(field.Id,out value))return value;return BaselineValue(id,field);}
        public void SetList(string id,string template,string display)
        {
            template=(template??"").Trim();display=(display??"").Trim();if(!CollectionRules.SafeItem(template)||display.Any(Char.IsControl)||display.Length>128)throw new FormatException(Msg.Key("err_vorlage_oder_anzeigename_ist"));var source=UpstreamItems();bool owned=!source.ContainsKey(id);var item=Ensure(id,owned);var old=ResourceListValue.Parse(owned?item.ListValue:source[id]);old.Template=template;old.Display=display;string raw=old.Render();if(!owned&&raw==source[id]){item.ListValue="";if(item.Fields.Count==0)Overrides.Items.Remove(id);}else item.ListValue=raw;
        }
        public void SetField(string id,LocalDetailField field,string value)
        {value=field.Normalize(value);var source=UpstreamItems();bool owned=!source.ContainsKey(id);ResourceItemOverride item=Ensure(id,owned);string original=BaselineValue(id,field);if(value.Length==0||!owned&&value==original)item.Fields.Remove(field.Id);else item.Fields[field.Id]=value;if(!item.Owned&&item.ListValue.Length==0&&item.Fields.Count==0)Overrides.Items.Remove(id);}
        public void Add(string id,string template,string display,string transport)
        {var previous=Overrides.Clone();try{id=(id??"").Trim().ToLowerInvariant();if(!CollectionRules.SafeItem(id))throw new FormatException(Msg.Key("err_ungueltige_ressourcenkennung", id));if(Items().Any(x=>x.Id.Equals(id,StringComparison.OrdinalIgnoreCase)))throw new FormatException(Msg.Key("err_ressource_ist_bereits_vorhanden", id));if(Items().Count>=Spec.MaximumItems)throw new FormatException(Msg.Key("err_hoechstens_ressourcen_sind_erlaubt", Spec.MaximumItems));var item=Ensure(id,true);item.ListValue=new ResourceListValue{Template=(template??"").Trim(),Display=(display??"").Trim()}.Render();if(!CollectionRules.SafeItem(ResourceListValue.Parse(item.ListValue).Template))throw new FormatException(Msg.Key("err_ungueltige_vorbildressource"));LocalDetailField field=Spec.Fields.FirstOrDefault(x=>x.Scope=="custom"&&x.Key.Equals("transport",StringComparison.OrdinalIgnoreCase));if(field!=null&&!String.IsNullOrWhiteSpace(transport))item.Fields[field.Id]=field.Normalize(transport);Validate();}catch{Overrides=previous;throw;}}
        public void Remove(string id){ResourceItemOverride item;if(!Overrides.Items.TryGetValue(id,out item)||!item.Owned)throw new FormatException(Msg.Key("err_originalressource_kann_nicht_geloescht", id));Overrides.Items.Remove(id);Validate();}
        public void StageDependencyWrites(IDictionary<string,byte[]> writes)
        {foreach(var pair in writes){if(!Before.ContainsKey(pair.Key))Before[pair.Key]=SafeFiles.HashFile(pair.Key);dependentWrites[pair.Key]=pair.Value;}}
        public void Reset(){Overrides=new ResourceOverrideStore();dependentWrites.Clear();Validate();Generation++;}
        // Counts resets and saves, so attached editors (the text pack, 0.4.30) know when to reload.
        public int Generation{get;private set;}
        public bool Dirty{get{return Overrides.Render()!=initialOverrides||dependentWrites.Count>0||!ExternalLoader&&LoaderChanged;}}
        // ---- the loader's own switch, [plugins] <plugin> in tesmioloader.ini ----
        public string LoaderIni{get;private set;}
        public bool LoaderEnabled{get;private set;}
        bool ReadLoaderSwitch(){try{return !File.Exists(LoaderIni)||new Ini(SafeFiles.Text(LoaderIni)).Get("plugins",Spec.Plugin,"1")!="0";}catch(FormatException){return true;}}
        public bool LoaderChanged{get{return LoaderEnabled!=ReadLoaderSwitch();}}
        // Whether the INI's own activity key (for example [needs] enabled) is on; true
        // when the schema has no such key.
        public bool ActivityOn{get{if(Spec.ActiveSection.Length==0)return true;string value=new LooseIni(Effective()).Get(Spec.ActiveSection,Spec.ActiveKey);return Spec.ActiveValues.Contains(value,StringComparer.OrdinalIgnoreCase);}}
        // The single "plugin active" switch: loaded by TesmioLoader AND switched on in its INI.
        public bool SwitchOn{get{return LoaderEnabled&&ActivityOn;}}
        public void SetLoaderEnabled(bool enabled)
        {
            LoaderEnabled=enabled;
            // Switching on also flips the INI's activity key, otherwise the DLL would
            // load and decline; switching off leaves the INI alone.
            LocalDetailField activity=Spec.ActivityField;
            if(enabled&&activity!=null&&!ActivityOn&&Spec.ActiveValues.Length>0)SetGlobal(activity,Spec.ActiveValues[0]);
        }
        byte[] RenderLoaderIni()
        {
            var change=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{Ini.Id("plugins",Spec.Plugin),LoaderEnabled?"1":"0"}};
            string text=File.Exists(LoaderIni)?new Ini(SafeFiles.Text(LoaderIni)).Render(change):"[plugins]\r\n"+Spec.Plugin+"="+(LoaderEnabled?"1":"0")+"\r\n";
            return SafeFiles.Utf8.GetBytes(text);
        }
        public string Effective()
        {
            var doc=new LooseIni(upstreamText);var source=UpstreamItems();foreach(var item in Overrides.Items.Values.OrderBy(x=>x.Id,StringComparer.OrdinalIgnoreCase))
            {
                if(Spec.IsSections)
                {
                    // A hidden original loses its whole section; a personal one gets a fresh
                    // section with its keys in schema order; an overridden original keeps its
                    // section and comments and only the changed keys are rewritten.
                    string sectionName=SectionOf(item.Id);
                    if(item.Suppressed&&source.ContainsKey(item.Id)){doc.RemoveSection(sectionName);continue;}
                    if(item.Owned&&!source.ContainsKey(item.Id))doc.EnsureSection(sectionName);
                    foreach(LocalDetailField field in Spec.ItemFields(Spec.GroupOfId(item.Id))){string value;if(item.Fields.TryGetValue(field.Id,out value)){if(field.Type=="lines")doc.SetAll(sectionName,field.Key,value.Split('\n'));else doc.Set(sectionName,field.Key,value);}}
                    continue;
                }
                if(item.Suppressed&&source.ContainsKey(item.Id)){doc.Remove(Spec.ListSection,item.Id);continue;}if(item.Owned&&!source.ContainsKey(item.Id))doc.Set(Spec.ListSection,item.Id,item.ListValue);else if(item.ListValue.Length>0)doc.Set(Spec.ListSection,item.Id,item.ListValue);foreach(var pair in item.Fields){LocalDetailField field=Spec.Fields.First(x=>x.Id.Equals(pair.Key,StringComparison.OrdinalIgnoreCase));if(field.Scope=="custom")doc.Set(Spec.ItemSectionPrefix+item.Id,field.Key,pair.Value);else doc.Set(field.Section,item.Id,pair.Value);}
            }
            foreach(var g in Overrides.Globals){LocalDetailField field=Spec.Fields.First(x=>x.Id.Equals(g.Key,StringComparison.OrdinalIgnoreCase));doc.Set(field.Section,field.Key,g.Value);}
            return doc.Render();
        }
        public void Validate()
        {
            var source=UpstreamItems();foreach(var change in Overrides.Items.Values){if(change.Owned){if(source.ContainsKey(change.Id)||change.ListValue.Length==0&&!Spec.IsSections||Spec.IsSections&&Spec.IsReserved(change.Id))throw new FormatException(Msg.Key("err_persoenlicher_eintrag_kollidiert_mit", change.Id));}else if(!source.ContainsKey(change.Id))throw new FormatException(Msg.Key("err_original_eintrag_nicht_mehr", change.Id));if(change.Suppressed&&!Spec.HidesOriginals)throw new FormatException(Msg.Key("err_ausblenden_gibt_es_nur", change.Id));if(change.ListValue.Length>0){if(Spec.IsSections)throw new FormatException(Msg.Key("err_abschnittseintraege_haben_keine_listenzeile", change.Id));if(Spec.IsList)Spec.NormalizeTuple(change.ListValue);else{var list=ResourceListValue.Parse(change.ListValue);if(!CollectionRules.SafeItem(list.Template)||list.Display.Length>128||list.Display.Any(Char.IsControl))throw new FormatException(Msg.Key("err_ungueltiger_listeneintrag", change.Id));}}foreach(var pair in change.Fields){LocalDetailField field=Spec.Fields.FirstOrDefault(x=>x.Id.Equals(pair.Key,StringComparison.OrdinalIgnoreCase));if(field==null||field.Scope=="global")throw new FormatException(Msg.Key("err_unbekanntes_detailfeld", pair.Key));field.Normalize(pair.Value);}}
            foreach(var g in Overrides.Globals){LocalDetailField field=Spec.Fields.FirstOrDefault(x=>x.Id.Equals(g.Key,StringComparison.OrdinalIgnoreCase)&&x.Scope=="global");if(field==null)throw new FormatException(Msg.Key("err_unbekanntes_plugin_feld", g.Key));field.Normalize(g.Value);}
            if(!Spec.IsList)foreach(var item in Items()){if(item.Template.Equals("custom",StringComparison.OrdinalIgnoreCase)){LocalDetailField transport=Spec.Fields.FirstOrDefault(x=>x.Scope=="custom"&&x.Key.Equals("transport",StringComparison.OrdinalIgnoreCase));if(transport!=null&&String.IsNullOrWhiteSpace(Value(item.Id,transport)))throw new RuleException("resource_custom_needs_transport",item.Id);}}
            CheckReferences();
            if(Spec.IsSections)
            {
                var items=Items();
                // unique = 1 fields: the deposit type number is keyed on by the engine.
                foreach(LocalDetailField field in Spec.Fields.Where(x=>x.Scope=="item"&&x.Unique))
                {var seen=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(var item in items){if(!FieldApplies(field,item.Id))continue;string value=Value(item.Id,field);if(value.Length==0)continue;string other;if(seen.TryGetValue(value,out other))throw new RuleException("section_value_taken",field.Label+" = "+value+" ("+other+", "+item.Id+")");seen[value]=item.Id;}}
                // length_rule: the editor brush name is shorter under map = terrain.
                foreach(LocalDetailField field in Spec.Fields.Where(x=>x.Scope=="item"&&x.LengthRuleKey.Length>0))
                {LocalDetailField other=Spec.Fields.FirstOrDefault(x=>x.Scope=="item"&&x.Key.Equals(field.LengthRuleKey,StringComparison.OrdinalIgnoreCase));if(other==null)continue;foreach(var item in items){if(!FieldApplies(field,item.Id))continue;if(Value(item.Id,other).Equals(field.LengthRuleValue,StringComparison.OrdinalIgnoreCase)&&Value(item.Id,field).Length>field.LengthRuleLimit)throw new RuleException("section_length_rule",item.Id+": "+field.Label+" > "+field.LengthRuleLimit+" ("+other.Label+" = "+field.LengthRuleValue+")");}}
                var sections=new LooseIni(Effective()).SectionNames().Where(x=>!Spec.IsReserved(x)&&(Spec.GroupOfId(x)!=null||Spec.SectionPrefix.Length==0||x.StartsWith(Spec.SectionPrefix,StringComparison.OrdinalIgnoreCase))).ToList();if(sections.Count>Spec.MaximumItems||sections.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=sections.Count)throw new FormatException(Msg.Key("err_ungueltige_wirksame_abschnittsliste"));
                return;
            }
            var effective=new LooseIni(Effective());var ids=effective.Entries(Spec.ListSection);if(!Spec.IsList&&ids.Count==0||ids.Count>Spec.MaximumItems||ids.Select(x=>x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=ids.Count)throw new FormatException(Msg.Key("err_ungueltige_wirksame_ressourcenliste"));
        }
        // Reference checks (0.4.40): entry ids and field values that must name something the
        // game knows (research ids, building files, text ids). Only the player's own entries
        // and personal values are checked, and only once the form has handed over the sets;
        // a set that cannot be read (no game folder) is skipped rather than flagging everything.
        public ReferenceSets References;
        void CheckReferences()
        {
            if(References==null)return;
            foreach(var item in Items())
            {
                ItemGroup group=Spec.GroupOfId(item.Id);string set=group==null?Spec.IdReference:group.IdReference;
                if(set.Length==0||!item.Owned)continue;
                string id=Spec.DisplayId(item.Id);
                if(!References.Contains(set,id))throw new RuleException(set=="game_texts"?"reference_text_id":set=="game_research"?"reference_research_id":set=="resources"?"reference_resource_id":"reference_unknown_id",id);
            }
            if(!Spec.IsSections)return;
            foreach(LocalDetailField field in Spec.Fields.Where(x=>x.Scope=="item"&&x.Reference.Length>0))
            {
                HashSet<string> own=OwnReferenceIds(field.ReferenceOwn);
                foreach(var item in Items())
                {
                    if(!FieldApplies(field,item.Id))continue;
                    ResourceItemOverride change;bool personal=Overrides.Items.TryGetValue(item.Id,out change)&&change.Fields.ContainsKey(field.Id);
                    if(!item.Owned&&!personal)continue;
                    if(field.Reference=="files")
                    {
                        // 0.4.48: the value names a file under the field's picker folders; dds_dxt1 / dds_dxt5 also check the header.
                        string rel=Value(item.Id,field).Trim();if(rel.Length==0)continue;
                        string label=References.Language==null?field.Label:Spec.FieldLabel(References.Language,field);
                        string found=PickerFile(field,rel);if(found==null)throw new RuleException("reference_file_missing",Spec.DisplayId(item.Id),label,rel);
                        string problem=ReferenceSets.DdsProblem(found,field.ReferenceFormat);if(problem!=null)throw new RuleException("reference_file_"+problem,Spec.DisplayId(item.Id),label,rel);
                        continue;
                    }
                    foreach(string id in ReferenceSets.IdsOf(field,Value(item.Id,field)))
                        if(!(own.Contains(id)||References.Contains(field.Reference,id)))
                            throw new RuleException(field.Reference=="game_buildings"?"reference_building":field.Reference=="game_texts"?"reference_text":field.Reference=="resources"?"reference_resource":"reference_research",Spec.DisplayId(item.Id),(References.Language==null?field.Label:Spec.FieldLabel(References.Language,field)),id);
                }
            }
        }
        // A file a picker = files field names: relative to one of its picker folders, no climbing (0.4.48).
        public string PickerFile(LocalDetailField field,string rel)
        {
            if(rel.Contains("..")||rel.Contains(":")||rel.StartsWith("/")||rel.StartsWith("\\"))return null;
            foreach(string spec in field.PickerFolders){string root;try{root=Spec.ResolvePath(spec,Build);}catch(FormatException){continue;}string path=Path.Combine(root,rel.Replace('/','\\'));if(File.Exists(path))return path;}
            return null;
        }
        // The player's own entries of a group count as known targets unless switched off.
        HashSet<string> OwnReferenceIds(string groupId)
        {
            var result=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ItemGroup group=Spec.GroupById(groupId);if(group==null)return result;
            LocalDetailField enabled=Spec.ItemFields(group).FirstOrDefault(x=>x.Type=="boolean"&&x.Key.Equals("enabled",StringComparison.OrdinalIgnoreCase));
            foreach(var item in Items(group))if(enabled==null||Value(item.Id,enabled)!="0")result.Add(Spec.DisplayId(item.Id));
            return result;
        }
        public void AssertUnchanged(){Spec.AssertUnchanged();foreach(var pair in Before){SafeFiles.NoLinks(pair.Key);if(SafeFiles.HashFile(pair.Key)!=pair.Value)throw new IOException(Msg.Key("err_datei_inzwischen_geaendert_neu_2", pair.Key));}}
        public string Commit(Action guard)
        {
            string mutexName="Local\\TesmioAutoload_"+SafeFiles.Hash(SafeFiles.Utf8.GetBytes(Build.ToUpperInvariant()));using(var mutex=new Mutex(false,mutexName)){bool held=false;try{try{held=mutex.WaitOne(0);}catch(AbandonedMutexException){held=true;}if(!held)throw new IOException(Msg.Key("err_ein_anderes_autoload_fenster"));guard();AssertUnchanged();Validate();string effective=Effective(),personal=Overrides.Render();var writes=new Dictionary<string,byte[]>(dependentWrites,StringComparer.OrdinalIgnoreCase);writes[UserIni]=SafeFiles.Utf8.GetBytes(personal);if(!ExternalLoader&&LoaderChanged)writes[LoaderIni]=RenderLoaderIni();if(refreshUpstream||!File.Exists(UpstreamFile))writes[UpstreamFile]=SafeFiles.Utf8.GetBytes(upstreamText);writes[LocalIni]=SafeFiles.Utf8.GetBytes(effective);writes[Receipt]=SafeFiles.Utf8.GetBytes("[state]\r\nid = "+Spec.Id+"\r\neffective_hash = "+SafeFiles.Hash(SafeFiles.Utf8.GetBytes(effective))+"\r\nupstream_hash = "+SafeFiles.Hash(SafeFiles.Utf8.GetBytes(upstreamText))+"\r\nschema_hash = "+Spec.SchemaHash+"\r\n");string backup=Transaction.Apply(Build,Spec.Id,writes,Before,guard);foreach(string path in Before.Keys.ToList())Before[path]=SafeFiles.HashFile(path);initialOverrides=personal;dependentWrites.Clear();refreshUpstream=false;Generation++;return backup;}finally{if(held)mutex.ReleaseMutex();}}
        }
    }

    public static class LocalResourceRuntime
    {
        public static bool Active(CatalogEntry entry,string build)
        {try{var spec=LocalEditorSpec.Load(entry.Root);string root=Path.GetFullPath(build),dll=SafeFiles.Child(root,"plugins\\"+spec.Plugin+".dll"),ini=SafeFiles.Child(root,"plugins\\"+spec.ConfigName);if(!File.Exists(dll)||!File.Exists(ini))return false;string loader=SafeFiles.Child(root,"tesmioloader.ini");if(File.Exists(loader)&&new Ini(SafeFiles.Text(loader)).Get("plugins",spec.Plugin,"1")=="0")return false;if(spec.ActiveSection.Length>0){string value=new LooseIni(SafeFiles.Text(ini)).Get(spec.ActiveSection,spec.ActiveKey);if(!spec.ActiveValues.Contains(value,StringComparer.OrdinalIgnoreCase))return false;}return true;}catch{return false;}}
    }
    public static class LocalResourceGuard
    {
        public static void Check(LocalResourceSession session)
        {SafeFiles.NoLinks(session.Build);foreach(var process in Process.GetProcesses())using(process){string name;try{name=process.ProcessName;}catch(InvalidOperationException){continue;}if(name.StartsWith("SOVIET",StringComparison.OrdinalIgnoreCase)||name.Equals("tesmiolauncher",StringComparison.OrdinalIgnoreCase))throw new IOException(Msg.Key("err_zuerst_spiel_und_tesmiolauncher", name));}foreach(string file in session.ExternalLoader?new[]{"tesmioloader.dll","tesmiolauncher.exe"}:new[]{"tesmioloader.dll","tesmiolauncher.exe","plugins\\"+session.Spec.Plugin+".dll"})if(!File.Exists(SafeFiles.Child(session.Build,file)))throw new IOException(Msg.Key("err_benoetigte_lokale_datei_fehlt", file));string cfg=SafeFiles.Child(session.Build,"tesmioloader.ini");Ini settings=File.Exists(cfg)?new Ini(SafeFiles.Text(cfg)):new Ini("");if(settings.Get("tesmioloader","plugins","1")=="0")throw new IOException(Msg.Key("err_plugins_sind_im_vorhandenen"));}
    }
}
