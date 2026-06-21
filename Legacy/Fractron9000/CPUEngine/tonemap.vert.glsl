
void main()
{
	gl_Position = gl_ModelViewProjectionMatrix * vec4(gl_Vertex.xy, 0.0, 1.0);
	gl_TexCoord[0] = gl_MultiTexCoord0;
	gl_FrontColor = vec4(1,1,1,1);
}
