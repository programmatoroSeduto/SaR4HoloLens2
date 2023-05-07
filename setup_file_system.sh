unity_base_path="$(pwd)"
unity_assets_path="${unity_base_path}/Assets"

echo "Assets path is: ${unity_assets_path}"


cd "$unity_assets_path"

######## SCRIPTING ########
echo "Creating scripting folder ... "
mkdir Scripts
cd Scripts

mkdir _Packages
cd _Packages
touch .gitkeep
cd ..

mkdir Types
cd Types
touch .gitkeep
cd ..

mkdir Components
cd Components
touch .gitkeep
cd ..

mkdir Utils
cd Utils
touch .gitkeep
cd ..

cd ..
echo "Creating scripting folder ... OK"


######## DLL SCRIPTING ########
echo "Creating DLL assets folder ... "
mkdir DLLFiles
cd DLLFiles
touch .gitkeep
cd ..
echo "Creating DLL assets folder ... OK"


######## RESOURCES FOLDER ########
echo "Creating Resources folder ... "
mkdir Resources
cd Resources

mkdir VisualAPIBaseScripts
cd VisualAPIBaseScripts

mkdir Types
cd Types
touch .gitkeep
cd ..

mkdir Components
cd Components
touch .gitkeep
cd ..

mkdir Utils
cd Utils
touch .gitkeep
cd ..

cd ..

cd ..
echo "Creating Resources folder ... OK"