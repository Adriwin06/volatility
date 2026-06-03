using System.Reflection;

using Volatility.Operations.Resources;
using Volatility.Resources;
using Volatility.Utilities;

using static Volatility.Utilities.TypeUtilities;
using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.CLI.Commands;

internal class AutotestCommand : ICommand
{
    public static string CommandToken => "autotest";
    public static string CommandDescription => "Runs automatic tests to ensure the application is working." +
        " When provided a path & format, will import, export, then reimport specified file to ensure IO parity.";
    public static string CommandParameters => "[--format=<tub,bpr,x360,ps3>] [--path=<file path>]";

    public string? Format { get; set; }
    public string? Path { get; set; }

    public async Task Execute()
    {
        if (!string.IsNullOrEmpty(Path))
        {
            TextureBase? header = Format switch
            {
                "BPR" => new TextureBPR(Path),
                "TUB" => new TexturePC(Path),
                "X360" => new TextureX360(Path),
                "PS3" => new TexturePS3(Path),
                _ => throw new InvalidPlatformException(),
            };

            header.PullAll();

            TestHeaderRW($"autotest_{System.IO.Path.GetFileName(Path)}", header);

            return;
        }

        /*
         * Right now, the autotest simply creates
         * example texture classes akin to what the parser
         * will interpret from an input format, then write
         * them out to various platform formatted header files.
         */
            
        // TUB Texture data test case
        TexturePC textureHeaderPC = new()
        {
            AssetName = "autotest_header_PC",
            ResourceID = ResourceID.HashFromString("autotest_header_PC"),
            Format = D3DFORMAT.D3DFMT_DXT1,
            Width = 1024,
            Height = 512,
            MipmapLevels = 11,
            UsageFlags = TextureBaseUsageFlags.GRTexture
        };

        TestHeaderRW("autotest_header_PC.dat", textureHeaderPC);

        // BPR Texture data test case
        TextureBPR textureHeaderBPR = new()
        {
            AssetName = "autotest_header_BPR",
            ResourceID = ResourceID.HashFromString("autotest_header_BPR"),
            Format = DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM,
            Width = 1024,
            Height = 512,
            MipmapLevels = 11,
            UsageFlags = TextureBaseUsageFlags.GRTexture
        };

        // SKIPPING BPR IMPORT AS IT'S NOT SUPPORTED YET

        // Write 32 bit test BPR header
        TestHeaderRW("autotest_header_BPR.dat", textureHeaderBPR);

        textureHeaderBPR.SetResourceArch(Arch.x64);
        textureHeaderBPR.AssetName = "autotest_header_BPRx64";
        textureHeaderBPR.ResourceID = ResourceID.HashFromString(textureHeaderBPR.AssetName);

        // Write 64 bit test BPR header
        TestHeaderRW("autotest_header_BPRx64.dat", textureHeaderBPR);

        // PS3 Texture data test case
        TexturePS3 textureHeaderPS3 = new()
        {
            AssetName = "autotest_header_PS3",
            ResourceID = ResourceID.HashFromString("autotest_header_PS3"),
            Format = CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45,
            Width = 1024,
            Height = 512,
            MipmapLevels = 11,
            UsageFlags = TextureBaseUsageFlags.GRTexture
        };
        textureHeaderPS3.PushAll();
        TestHeaderRW("autotest_header_PS3.dat", textureHeaderPS3);

        // X360 Texture data test case
        TextureX360 textureHeaderX360 = new()
        {
            AssetName = "autotest_header_X360",
            ResourceID = ResourceID.HashFromString("autotest_header_X360"),
            Format = new()
            {
                Tiled = true,
                SwizzleW = GPUSWIZZLE.GPUSWIZZLE_W,
                SwizzleX = GPUSWIZZLE.GPUSWIZZLE_X,
                SwizzleY = GPUSWIZZLE.GPUSWIZZLE_Y,
                SwizzleZ = GPUSWIZZLE.GPUSWIZZLE_Z,
            },
            Width = 1024,
            Height = 512,
            Depth = 1,
            MipmapLevels = 11,
            UsageFlags = TextureBaseUsageFlags.GRTexture
        };
        textureHeaderX360.PushAll();
        TestHeaderRW("autotest_header_X360.dat", textureHeaderX360);

        await TestResourceGraphRW();

        // File name endian flip test case
        string endianFlipTestName = "12_34_56_78_texture.dat";
        Console.WriteLine($"AUTOTEST - Endian Test: Flipped endian {endianFlipTestName} to {FlipPathResourceIDEndian(endianFlipTestName)}");
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Format = (args.TryGetValue("format", out object? format) ? format as string : "auto").ToUpper();
        Path = args.TryGetValue("path", out object? path) ? path as string : "";
    }

    public void TestHeaderRW(string name, TextureBase header, bool skipImport = false) 
    {
        using (FileStream fs = new(name, FileMode.Create))
        {
            // We don't want the command runner to catch the error
            try
            {
                header.PushAll();
            }
            catch (NotImplementedException)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"A push isn't implemented for {header.GetType().Name}!");
                Console.ResetColor();
            }

            using (ResourceBinaryWriter writer = new(fs, header.ResourceEndian))
            {
                Console.WriteLine($"AUTOTEST - Writing autotest {name} to working directory...");
                header.WriteToStream(writer);
                writer.Close();
            }

            if (skipImport)
                return;
            
            TextureBase? newHeader = Activator.CreateInstance(
                header.GetType(),
                fs.Name,
                Endian.Agnostic) as TextureBase;

            try
            {
                newHeader?.PullAll();
            }
            catch (NotImplementedException)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"A pull isn't implemented for {newHeader?.GetType().Name}!");
                Console.ResetColor();
            }

            TestCompareHeaders(header, newHeader);
        }
    }

    public static void TestCompareHeaders(object exported, object imported)
    {
        Type type = exported.GetType();

        Console.WriteLine(">> Comparing properties and fields of " + type.Name + ":");
    
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        int mismatches = 0;
        foreach (PropertyInfo property in properties)
        {
            
            object value1 = property.GetValue(exported, null);
            object value2 = property.GetValue(imported, null);
    
            if (IsComplexType(property.PropertyType))
            {
                Console.WriteLine($" >  Inspecting nested type {property.Name}:");
                TestCompareHeaders(value1, value2);
                Console.WriteLine($" >  Finished inspecting nested type {property.Name}");
            }
            else if (!Equals(value1, value2))
            {
                mismatches++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Mismatch - {property.Name}: Exported = {value1}, Imported = {value2}");
                Console.ResetColor();
            }
        }
    
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            object value1 = field.GetValue(exported);
            object value2 = field.GetValue(imported);
    
            if (IsComplexType(field.FieldType))
            {
                Console.WriteLine($" >  Inspecting nested type {field.Name}:");
                TestCompareHeaders(value1, value2);
                Console.WriteLine($" >  Finished inspecting nested type {field.Name}");
            }
            else if (!Equals(value1, value2))
            {
                mismatches++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Mismatch - {field.Name}: Exported = {value1}, Imported = {value2}");
                Console.ResetColor();
            }
        }

        if (mismatches == 0) 
            Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine(">> Finished Comparing properties and fields of " + type.Name + $" - {mismatches} mismatches");
        Console.ResetColor();
    }

    private static async Task TestResourceGraphRW()
    {
        string tempDir = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "volatility_autotest_resource_graph_" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDir);
        Console.WriteLine($"AUTOTEST - Resource graph smoke tests writing to {tempDir}");

        try
        {
            foreach (Platform platform in new[] { Platform.BPR, Platform.X360 })
            {
                await TestResourceGraphRW(tempDir, platform);
            }
        }
        finally
        {
            string fullTempDir = System.IO.Path.GetFullPath(tempDir);
            string fullTempRoot = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath());
            if (fullTempDir.StartsWith(fullTempRoot, StringComparison.OrdinalIgnoreCase)
                && Directory.Exists(fullTempDir))
            {
                Directory.Delete(fullTempDir, recursive: true);
            }
        }
    }

    private static async Task TestResourceGraphRW(string tempDir, Platform platform)
    {
        ResourceImport modelRef = ExternalImport($"autotest_{platform}_model");
        ResourceImport secondaryModelRef = ExternalImport($"autotest_{platform}_secondary_model");
        ResourceImport graphicsSpecRef = ExternalImport($"autotest_{platform}_graphics_spec");
        ResourceImport wheelSpecRef = ExternalImport($"autotest_{platform}_wheel_graphics_spec");

        GraphicsStub graphicsStub = new()
        {
            AssetName = $"autotest_{platform}_graphics_stub",
            ResourceID = ResourceID.HashFromString($"autotest_{platform}_graphics_stub"),
            VehicleGraphicsIndex = 1,
            WheelGraphicsIndex = 0,
            Unknown0 = 7,
            Unknown1 = 9,
            GraphicsSpecReference = graphicsSpecRef,
            WheelGraphicsSpecReference = wheelSpecRef,
        };
        GraphicsStub importedStub = await RoundTripResource(tempDir, "graphics_stub", platform, graphicsStub);
        AssertEqual("GraphicsStub.VehicleGraphicsIndex", graphicsStub.VehicleGraphicsIndex, importedStub.VehicleGraphicsIndex);
        AssertImport("GraphicsStub.GraphicsSpecReference", graphicsSpecRef, importedStub.GraphicsSpecReference);
        AssertImport("GraphicsStub.WheelGraphicsSpecReference", wheelSpecRef, importedStub.WheelGraphicsSpecReference);

        WheelGraphicsSpec wheelSpec = new()
        {
            AssetName = $"autotest_{platform}_wheel_spec",
            ResourceID = ResourceID.HashFromString($"autotest_{platform}_wheel_spec"),
            Version = 1,
            WheelModelIndex = 0,
            CaliperModelIndex = 1,
            Unknown0 = 3,
            WheelModelReference = modelRef,
            CaliperModelReference = secondaryModelRef,
        };
        WheelGraphicsSpec importedWheelSpec = await RoundTripResource(tempDir, "wheel_graphics_spec", platform, wheelSpec);
        AssertEqual("WheelGraphicsSpec.CaliperModelIndex", wheelSpec.CaliperModelIndex, importedWheelSpec.CaliperModelIndex);
        AssertImport("WheelGraphicsSpec.WheelModelReference", modelRef, importedWheelSpec.WheelModelReference);
        AssertImport("WheelGraphicsSpec.CaliperModelReference", secondaryModelRef, importedWheelSpec.CaliperModelReference);

        GraphicsSpec graphicsSpec = new()
        {
            AssetName = $"autotest_{platform}_graphics_spec",
            ResourceID = ResourceID.HashFromString($"autotest_{platform}_graphics_spec"),
            Version = 3,
            Parts =
            [
                new GraphicsPart
                {
                    ModelReference = modelRef,
                    PartLocator = Matrix44.Identity,
                    PartVolumeId = 4,
                    NumRigidBodiesForPart = 1,
                    RigidBodyToSkinMatrixTransforms = [Matrix44.Identity],
                },
            ],
            ShatteredGlassParts =
            [
                new ShatteredGlassPart
                {
                    ModelReference = secondaryModelRef,
                    ModelIndex = 1,
                    BodyPartIndex = 2,
                    BodyPartType = 3,
                },
            ],
        };
        GraphicsSpec importedGraphicsSpec = await RoundTripResource(tempDir, "graphics_spec", platform, graphicsSpec);
        AssertEqual("GraphicsSpec.Parts.Count", 1, importedGraphicsSpec.Parts.Count);
        AssertEqual("GraphicsSpec.ShatteredGlassParts.Count", 1, importedGraphicsSpec.ShatteredGlassParts.Count);
        AssertEqual("GraphicsSpec.PartVolumeId", graphicsSpec.Parts[0].PartVolumeId, importedGraphicsSpec.Parts[0].PartVolumeId);
        AssertImport("GraphicsSpec.Part.ModelReference", modelRef, importedGraphicsSpec.Parts[0].ModelReference);
        AssertImport("GraphicsSpec.ShatteredGlass.ModelReference", secondaryModelRef, importedGraphicsSpec.ShatteredGlassParts[0].ModelReference);

        PropGraphicsList propGraphicsList = new()
        {
            AssetName = $"autotest_{platform}_prop_graphics_list",
            ResourceID = ResourceID.HashFromString($"autotest_{platform}_prop_graphics_list"),
            ZoneNumber = 12,
            PropModels =
            [
                new PropGraphics
                {
                    TypeId = 0x101,
                    PropModelPointer = 0,
                    PartsPointer = 0,
                    ModelReference = modelRef,
                },
            ],
            PropPartModels =
            [
                new PropPartGraphics
                {
                    TypeId = 0x101,
                    PartId = 0x202,
                    PropModelPointer = 0,
                    ModelReference = secondaryModelRef,
                },
            ],
        };
        PropGraphicsList importedPropGraphicsList = await RoundTripResource(tempDir, "prop_graphics_list", platform, propGraphicsList);
        AssertEqual("PropGraphicsList.ZoneNumber", propGraphicsList.ZoneNumber, importedPropGraphicsList.ZoneNumber);
        AssertEqual("PropGraphicsList.PropModels.Count", 1, importedPropGraphicsList.PropModels.Count);
        AssertEqual("PropGraphicsList.PropPartModels.Count", 1, importedPropGraphicsList.PropPartModels.Count);
        AssertImport("PropGraphicsList.PropModel.Reference", modelRef, importedPropGraphicsList.PropModels[0].ModelReference);
        AssertImport("PropGraphicsList.PropPart.Reference", secondaryModelRef, importedPropGraphicsList.PropPartModels[0].ModelReference);

        PropInstanceData propInstanceData = new()
        {
            AssetName = $"autotest_{platform}_prop_instance_data",
            ResourceID = ResourceID.HashFromString($"autotest_{platform}_prop_instance_data"),
            ZoneNumber = 12,
            NumberOfPropInstanceDataPlusPropParts = 1,
            Instances =
            [
                new PropInstance
                {
                    WorldTransform = Matrix44.Identity,
                    TypeId = 0x101,
                    Unknown0 = 1,
                    Flags = 2,
                    InstanceId = 0x303,
                    AlternativeType = 0x404,
                    RotSpeed = 5,
                    MaxAngle = 6,
                    MinAngle = 7,
                    Padding = [0, 0, 0],
                },
            ],
            Cells =
            [
                new PropCellData
                {
                    First = 0,
                    Count = 1,
                    RunningValue = 1,
                },
            ],
        };
        PropInstanceData importedPropInstanceData = await RoundTripResource(tempDir, "prop_instance_data", platform, propInstanceData);
        AssertEqual("PropInstanceData.ZoneNumber", propInstanceData.ZoneNumber, importedPropInstanceData.ZoneNumber);
        AssertEqual("PropInstanceData.Instances.Count", 1, importedPropInstanceData.Instances.Count);
        AssertEqual("PropInstanceData.Cells.Count", 1, importedPropInstanceData.Cells.Count);
        AssertEqual("PropInstanceData.InstanceId", propInstanceData.Instances[0].InstanceId, importedPropInstanceData.Instances[0].InstanceId);

        StaticSoundMap staticSoundMap = new()
        {
            AssetName = $"autotest_{platform}_static_sound_map",
            ResourceID = ResourceID.HashFromString($"autotest_{platform}_static_sound_map"),
            Min = new Vector2(-1, -2),
            Max = new Vector2(3, 4),
            SubRegionWorldSize = 64,
            NumSubRegionsX = 1,
            NumSubRegionsZ = 1,
            RootType = 8,
            Entities =
            [
                new StaticSoundEntity
                {
                    Position = new Vector3(1, 2, 3),
                    Unknown0 = 4,
                    Unknown1 = 5,
                },
            ],
            SubRegions =
            [
                new StaticSoundSubRegion
                {
                    First = 0,
                    Count = 1,
                },
            ],
        };
        StaticSoundMap importedStaticSoundMap = await RoundTripResource(tempDir, "static_sound_map", platform, staticSoundMap);
        AssertEqual("StaticSoundMap.Entities.Count", 1, importedStaticSoundMap.Entities.Count);
        AssertEqual("StaticSoundMap.SubRegions.Count", 1, importedStaticSoundMap.SubRegions.Count);
        AssertEqual("StaticSoundMap.RootType", staticSoundMap.RootType, importedStaticSoundMap.RootType);

        IdList idList = new()
        {
            AssetName = $"autotest_{platform}_id_list",
            ResourceID = ResourceID.HashFromString($"autotest_{platform}_id_list"),
            Ids =
            [
                new ResourceImport(ResourceID.HashFromString($"autotest_{platform}_id_0")),
                new ResourceImport(ResourceID.HashFromString($"autotest_{platform}_id_1")),
            ],
        };
        IdList importedIdList = await RoundTripResource(tempDir, "id_list", platform, idList);
        AssertEqual("IdList.Ids.Count", 2, importedIdList.Ids.Count);
        AssertImportID("IdList.Ids[0]", idList.Ids[0], importedIdList.Ids[0]);
        AssertImportID("IdList.Ids[1]", idList.Ids[1], importedIdList.Ids[1]);

        PolygonSoupList polygonSoupList = new()
        {
            AssetName = $"autotest_{platform}_polygon_soup_list",
            ResourceID = ResourceID.HashFromString($"autotest_{platform}_polygon_soup_list"),
            Min = new Vector4(-1, -1, -1, 1),
            Max = new Vector4(1, 1, 1, 1),
            PolygonSoups =
            [
                new PolygonSoup
                {
                    Box = new PolygonSoupBox
                    {
                        Min = new Vector3(-1, -1, -1),
                        Max = new Vector3(1, 1, 1),
                        ValidMasks = -1,
                    },
                    VertexOffsets = [0, 0, 0],
                    CompressionGranularity = 0.25f,
                    Vertices =
                    [
                        new PolygonSoupVertex { X = 0, Y = 0, Z = 0 },
                        new PolygonSoupVertex { X = 100, Y = 0, Z = 0 },
                        new PolygonSoupVertex { X = 0, Y = 100, Z = 0 },
                    ],
                    Polygons =
                    [
                        new PolygonSoupPolygon
                        {
                            CollisionTag0 = 1,
                            CollisionTag1 = 2,
                            VertexIndices = [0, 1, 2],
                            EdgeCosines = [0, 0, 0, 0],
                        },
                    ],
                },
            ],
        };
        PolygonSoupList importedPolygonSoupList = await RoundTripResource(tempDir, "polygon_soup_list", platform, polygonSoupList);
        AssertEqual("PolygonSoupList.PolygonSoups.Count", 1, importedPolygonSoupList.PolygonSoups.Count);
        AssertEqual("PolygonSoupList.Vertices.Count", 3, importedPolygonSoupList.PolygonSoups[0].Vertices.Count);
        AssertEqual("PolygonSoupList.Polygons.Count", 1, importedPolygonSoupList.PolygonSoups[0].Polygons.Count);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"AUTOTEST - Resource graph smoke tests passed for {platform}");
        Console.ResetColor();
    }

    private static async Task<TResource> RoundTripResource<TResource>(
        string tempDir,
        string name,
        Platform platform,
        TResource resource)
        where TResource : Resource
    {
        string outputPath = System.IO.Path.Combine(tempDir, $"{platform}_{name}.bin");
        Console.WriteLine($"AUTOTEST - Writing {platform} {resource.ResourceType} resource graph test...");

        ExportResourceOperation exportOperation = new();
        await exportOperation.ExecuteAsync(resource, outputPath, platform);

        Resource imported = ResourceFactory.CreateResource(resource.ResourceType, platform, outputPath);
        if (imported is not TResource typed)
        {
            throw new InvalidDataException(
                $"Expected {typeof(TResource).Name} after round-trip, got {imported.GetType().Name}.");
        }

        return typed;
    }

    private static ResourceImport ExternalImport(string name)
    {
        return new ResourceImport(ResourceID.HashFromString(name), externalImport: true);
    }

    private static void AssertImport(string name, ResourceImport expected, ResourceImport actual)
    {
        AssertEqual($"{name}.ExternalImport", true, actual.ExternalImport);
        AssertImportID(name, expected, actual);
    }

    private static void AssertImportID(string name, ResourceImport expected, ResourceImport actual)
    {
        AssertEqual(
            $"{name}.ReferenceID",
            ResourceUtilities.ResolveResourceID(expected),
            ResourceUtilities.ResolveResourceID(actual));
    }

    private static void AssertEqual<T>(string name, T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
        {
            return;
        }

        throw new InvalidDataException($"{name} mismatch. Expected {expected}, got {actual}.");
    }

    public AutotestCommand() { }
}
