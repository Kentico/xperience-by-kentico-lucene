module.exports = function (grunt) {

    grunt.initConfig({
        clean: {
            formBuilder: ['wwwroot/Content/Bundles/Public/formComponents.css', 'wwwroot/Content/Bundles/Public/formComponents.min.css', 'wwwroot/Content/Bundles/Admin/formComponents.css', 'wwwroot/Content/Bundles/Admin/formComponents.min.css',
                'wwwroot/Content/Bundles/Public/formComponents.js', 'wwwroot/Content/Bundles/Public/formComponents.min.js'],
            pageBuilder: ['wwwroot/Content/Bundles/Public/pageComponents.css', 'wwwroot/Content/Bundles/Public/pageComponents.min.css', 'wwwroot/Content/Bundles/Admin/pageComponents.css', 'wwwroot/Content/Bundles/Admin/pageComponents.min.css',
                'wwwroot/Content/Bundles/Public/pageComponents.js', 'wwwroot/Content/Bundles/Public/pageComponents.min.js', 'wwwroot/Content/Bundles/Admin/pageComponents.js', 'wwwroot/Content/Bundles/Admin/pageComponents.min.js']
        },

        concat: {
            formBuilder: {
                files: {
                    // Styles - live site
                    'wwwroot/Content/Bundles/Public/formComponents.css': ['wwwroot/FormBuilder/Public/**/*.css'],
                    // Styles - admin
                    'wwwroot/Content/Bundles/Admin/formComponents.css': ['wwwroot/FormBuilder/Admin/**/*.css'],
                    // Scripts - live site and admin
                    'wwwroot/Content/Bundles/Public/formComponents.js': ['wwwroot/FormBuilder/Public/**/*.js']
                }
            },
            pageBuilder: {
                files: {
                    // Styles - live site
                    'wwwroot/Content/Bundles/Public/pageComponents.css': ['wwwroot/PageBuilder/Public/**/*.css'],
                    // Styles - admin
                    'wwwroot/Content/Bundles/Admin/pageComponents.css': ['wwwroot/PageBuilder/Admin/**/*.css'],
                    // Scripts - live site
                    'wwwroot/Content/Bundles/Public/pageComponents.js': ['wwwroot/PageBuilder/Public/**/*.js'],
                    // Scripts - admin
                    'wwwroot/Content/Bundles/Admin/pageComponents.js': ['wwwroot/PageBuilder/Admin/**/*.js']
                }
            }
        },

        cssmin: {
            formBuilder: {
                files: {
                    'wwwroot/Content/Bundles/Public/formComponents.min.css': 'wwwroot/Content/Bundles/Public/formComponents.css',
                    'wwwroot/Content/Bundles/Admin/formComponents.min.css': 'wwwroot/Content/Bundles/Admin/formComponents.css'
                }
            },
            pageBuilder: {
                files: {
                    'wwwroot/Content/Bundles/Public/pageComponents.min.css': ['wwwroot/Content/Bundles/Public/pageComponents.css'],
                    'wwwroot/Content/Bundles/Admin/pageComponents.min.css': ['wwwroot/Content/Bundles/Admin/pageComponents.css']
                }
            }
        },

        terser: {
            formBuilder: {
                files: {
                    'wwwroot/Content/Bundles/Public/formComponents.min.js': ['wwwroot/Content/Bundles/Public/formComponents.js']
                }
            },
            pageBuilder: {
                files: {
                    'wwwroot/Content/Bundles/Public/pageComponents.min.js': ['wwwroot/Content/Bundles/Public/pageComponents.js'],
                    'wwwroot/Content/Bundles/Admin/pageComponents.min.js': ['wwwroot/Content/Bundles/Admin/pageComponents.js']
                }
            }
        },

        less: {
            development: {
                files: {
                    'wwwroot/Content/Styles/Site.css': 'wwwroot/Content/Styles/Site.less',
                    'wwwroot/Content/Styles/Landing-page.css': 'wwwroot/Content/Styles/Landing-page.less'
                }
            }
        },
        watch: {
            styles: {
                files: ['wwwroot/Content/Styles/**/*.less'],
                tasks: ['less'],
                options: {
                    nospawn: true
                }
            }
        }
    });

    grunt.loadNpmTasks('grunt-contrib-clean');
    grunt.loadNpmTasks('grunt-contrib-concat');
    grunt.loadNpmTasks('grunt-contrib-cssmin');
    grunt.loadNpmTasks('grunt-contrib-watch');
    grunt.loadNpmTasks('grunt-contrib-less');
    grunt.loadNpmTasks('grunt-terser');

    grunt.registerTask('formBuilder', ['clean:formBuilder', 'concat:formBuilder', 'cssmin:formBuilder', 'terser:formBuilder']);
    grunt.registerTask('pageBuilder', ['clean:pageBuilder', 'concat:pageBuilder', 'cssmin:pageBuilder', 'terser:pageBuilder']);
    grunt.registerTask('default', ['less']);
};