const webpackMerge = require("webpack-merge");
const path = require("path");
const fs = require("fs");
const spawn = require("child_process").spawn;

const baseWebpackConfig = require("@kentico/xperience-webpack-config");

const { cert, key } = getDotnetCertPaths();

module.exports = (opts, argv) => {
  const baseConfig = (webpackConfigEnv, argv) => {
    return baseWebpackConfig({
      // Must match RegisterClientModule(org, project) on the backend
      // and AdminOrgName/ProjectName in the .csproj
      orgName: "kentico",
      projectName: "xperience-integrations-lucene-admin",
      webpackConfigEnv,
      argv,
    });
  };

  return new Promise((resolve) => {
    // Skip cert/key creation for prod builds or if the cert/key exist
    // See: https://github.com/dotnet/aspnetcore/issues/58330#issuecomment-2423338006
    if (
      argv.mode === "production" ||
      (fs.existsSync(cert) && fs.existsSync(key))
    ) {
      resolve(buildConfig(baseConfig, opts, argv));

      return;
    }

    console.info("Creating aspnet core dev-certs");
    spawn(
      "dotnet",
      [
        "dev-certs",
        "https",
        "--export-path",
        cert,
        "--format",
        "Pem",
        "--no-password",
      ],
      { stdio: "inherit" },
    ).on("exit", (code) => {
      resolve(buildConfig(baseConfig, opts, argv));
      if (code) {
        process.exit(code);
      }
    });
  });
};

function buildConfig(baseConfig, opts, argv) {
  const projectConfig = {
    devtool: argv.mode === "production" ? false : "inline-source-map",
    module: {
      rules: [
        {
          test: /\.(js|ts)x?$/,
          exclude: [/node_modules/],
          loader: "babel-loader",
        },
      ],
    },
    output: {
      clean: true,
      /**
       * Separate chunks cause Xperience's admin to fail when loading the bundle
       * ("Expected 1 file for source path '/adminresources/.../entry.js'").
       * This setting forces the output to be a single chunk.
       * https://github.com/webpack/webpack/issues/12464#issuecomment-1911309972
       */
      chunkFormat: false,
    },
    // Required when running the admin client module in 'Proxy' mode.
    // Must match CMSAdminClientModuleSettings in examples/DancingGoat/appsettings.Development.json
    devServer: {
      port: 3074,
      server: {
        type: "http",
        options: {
          key,
          cert,
        },
      },
    },
  };

  return webpackMerge.merge(projectConfig, baseConfig(opts, argv));
}

/**
 * Retrieves the ASP.NET Core dev certificate paths
 */
function getDotnetCertPaths() {
  const baseFolder =
    process.env.APPDATA !== undefined && process.env.APPDATA !== ""
      ? `${process.env.APPDATA}/ASP.NET/https`
      : `${process.env.HOME}/.aspnet/https`;
  fs.mkdirSync(baseFolder, { recursive: true });

  const certificateName = process.env.npm_package_name;

  const cert = path.join(baseFolder, `${certificateName}.pem`);
  const key = path.join(baseFolder, `${certificateName}.key`);

  return { cert, key };
}
