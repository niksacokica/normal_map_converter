using Pfim;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace normal_map_converter{
    class Program{

        private static int generatePngs( string path ){
            Bitmap green;
            foreach( string file in Directory.GetFiles( path ) ){
                if( file.EndsWith( "_d.dds" ) || file.EndsWith("_n.dds") ){
                    IImage dds = Pfim.Pfim.FromFile( file );
                    PixelFormat format = dds.Format switch{
                        Pfim.ImageFormat.Rgba32 => PixelFormat.Format32bppArgb,
                        _ => throw new NotImplementedException(),
                    };
                
                    IntPtr data = Marshal.UnsafeAddrOfPinnedArrayElement( dds.Data, 0 );
                    green = new( dds.Width, dds.Height, dds.Stride, format, data );
                    Bitmap blue = new( green.Width, green.Height, green.PixelFormat );

                    for( int i=0; i<green.Width; i++ ){
                        for( int j=0; j<green.Height; j++ ){
                            try{
                                Color c = green.GetPixel( i, j );

                                blue.SetPixel( i, j, file.EndsWith( "_d.dds" ) ? Color.FromArgb( 255, c.R/6, 64, 64 ) : Color.FromArgb( 255, c.A, 255-c.G, 255 ) );
                                if( file.EndsWith( "_d.dds" ) )
                                    green.SetPixel( i, j, Color.FromArgb( 255, c.R, c.G, c.B ) );
                            }catch( ArgumentOutOfRangeException e ){
                                Console.WriteLine( e.Message );
                                Environment.Exit( 1 );
                            }
                        }
                    }

                    if( file.EndsWith( "_d.dds" ) )
                        green.Save( Path.ChangeExtension( file, ".png" ), System.Drawing.Imaging.ImageFormat.Png );

                    blue.Save( file.Substring( 0, file.LastIndexOf( '.' ) - 2 ) + ( file.EndsWith("_d.dds") ? "_s.png" : "_n.png" ), System.Drawing.Imaging.ImageFormat.Png );
                }
                
                File.Delete( file );
                Console.WriteLine( file );
            }

            return 0;
        }

        private static int generateVmts( string path ){
            string vmtOpt = "\n\n\t\"$color2\"\t\"[0.5 0.5 0.5]\"" +
                "\n\t\"$blendTintByBaseAlpha\"\t\"1\"" +
                "\n\n\t\"$phong\"\t\"1\"" +
                "\n\t\"$phongboost\"\t\"1\"" +
                "\n\t\"$phongfresnelranges\"\t\"[0.05 0.115 0.945]\"" +
                "\n\n\t\"$normalmapalphaenvmapmask\"\t\"1\"" +
                "\n\n\t\"$envmapfresnel\"\t\"1\"" +
                "\n\t\"$envmaptint\"\t\"[.5 .5 .5]\"" +
                "\n}";

            string vmtDef = "\"VertexLitGeneric\"" +
                "\n{";

            foreach( string file in Directory.GetFiles( path ) ){
                if( file.EndsWith( "_d.vtf" ) ){
                    string paths = "\n\t\"$basetexture\"\t\"" + file.Substring( file.IndexOf( "materials" ) + 10, file.LastIndexOf('.') - file.LastIndexOf( "materials" ) - 10 ) + "\"" +
                        "\n\t\"$bumpmap\"\t\"" + file.Substring( file.IndexOf( "materials" ) + 10, file.LastIndexOf('.') - file.LastIndexOf( "materials" ) - 12 ) + "_n\"" +
                        "\n\t\"$phongexponenttexture\"\t\"" + file.Substring( file.IndexOf( "materials" ) + 10, file.LastIndexOf('.') - file.LastIndexOf( "materials" ) - 12 ) + "_s\"";

                    File.AppendAllText( file.Substring( 0, file.LastIndexOf('.') - 2 ) + ".vmt", vmtDef + paths + vmtOpt );
                }
            }

            return 0;
        }

        private static int generateQcs( string path ){
            Console.WriteLine( "Please enter a final path for the materials and models: " );
            string p = Console.ReadLine().Replace( "\\", "/" );
            p += p.EndsWith( "/" ) ? "" : "/";

            string qcAnim = "\"" +
                "\n\n$CBox 0 0 0 0 0 0" +
                "\n\n$Sequence \"idle\" {" +
                "\n\t\"anims\\idle.smd\"" +
                "\n\tfadein 0.2" +
                "\n\tfadeout 0.2" +
                "\n\tfps 1" +
                "\n\tloop" +
                "\n}";
            string qcPhys = "\"" +
                "\n{" +
	            "\n\t$automass" +
	            "\n\t$inertia 1" +
	            "\n\t$damping 0" +
	            "\n\t$rotdamping 0" +
	            "\n\t$rootbone \" \"" +
                "\n\t$concave" +
                "\n}";

            foreach ( string file in Directory.GetFiles( path ) ){
                if( file.EndsWith( "_phys.smd" ) ){
                    string name = Path.GetFileName( file );
                    string qc = "$ModelName \"" + p + name.Replace( "_phys.smd", "" ) + ".mdl\"" +
                        "\n\n$StaticProp" +
                        "\n\n$BodyGroup \"body\"" +
                        "\n{" +
                        "\n\tstudio \"" + name.Replace( "_phys", "" ) + "\"" +
                        "\n}";

                    Console.WriteLine( "What material you want the " + name.Replace( "_phys.smd", "" ) + " to be?" );
                    qc += "\n\n$SurfaceProp \"" + Console.ReadLine() + "\"" +
                        "\n\n$Contents \"solid\"" + 
                        "\n\n$MaxEyeDeflection 90" +
                        "\n\n$CDMaterials \"" + p.Replace( "/", "\\" ) +
                        qcAnim +
                        "\n\n$CollisionModel \"" + name +
                        qcPhys;

                    File.WriteAllText( file.Substring( 0, file.LastIndexOf('.') - 5 ) + ".qc", qc );
                }
            }

            return 0;
        }

        private static Dictionary<string, Func<string, int>> options = new Dictionary<string, Func<string, int>>(){
            { "pngs", generatePngs },
            { "vmts", generateVmts },
            { "qcs", generateQcs }
        };

        static void Main(){
            Console.WriteLine( "Please enter the full path of the folder:" );
            string path = Console.ReadLine(); ;

            while( !Directory.Exists( path ) ){
                Console.WriteLine( "That is an invalid folder path, please enter a valid folder path:" );

                path = Console.ReadLine();
            }

            Directory.SetCurrentDirectory( path );

            Console.WriteLine("Do you wish to generate \"pngs\"/\"qcs\"/\"vmts\"?");
            string option = Console.ReadLine();

            while( !options.ContainsKey( option ) ){
                Console.WriteLine( "Invalid option, possible options are: \"pngs\"/\"qcs\"/\"vmts\"." );
                
                option = Console.ReadLine();
            }

            options[option]( path );
        }
    }
}
