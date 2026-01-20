cmake_minimum_required(VERSION 3.16)
project(winPEAS_dotnet NONE)

set(PROJECT_FILE "${CMAKE_CURRENT_SOURCE_DIR}/winPEAS.csproj")

find_program(DOTNET_EXECUTABLE dotnet)
find_program(MSBUILD_EXECUTABLE msbuild)
find_program(XBUILD_EXECUTABLE xbuild)

if(DOTNET_EXECUTABLE)
  set(BUILD_TOOL "${DOTNET_EXECUTABLE}")
  set(BUILD_ARGS build "${PROJECT_FILE}" -c Release)
elseif(MSBUILD_EXECUTABLE)
  set(BUILD_TOOL "${MSBUILD_EXECUTABLE}")
  set(BUILD_ARGS "${PROJECT_FILE}" /p:Configuration=Release)
elseif(XBUILD_EXECUTABLE)
  set(BUILD_TOOL "${XBUILD_EXECUTABLE}")
  set(BUILD_ARGS "${PROJECT_FILE}" /p:Configuration=Release)
else()
  message(FATAL_ERROR "dotnet, msbuild, or xbuild is required to build winPEAS")
endif()

add_custom_target(winpeas ALL
  COMMAND ${BUILD_TOOL} ${BUILD_ARGS}
  WORKING_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}"
)
